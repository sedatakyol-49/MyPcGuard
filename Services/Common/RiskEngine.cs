using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Common;

public sealed class RiskEngine : IRiskEngine
{
    private static readonly string[] UserWritablePathMarkers =
    [
        "\\AppData\\",
        "/AppData/",
        "\\Temp\\",
        "/Temp/",
        "\\Downloads\\",
        "/Downloads/"
    ];

    private static readonly string[] HighRiskPathMarkers =
    [
        "\\AppData\\",
        "/AppData/",
        "\\Temp\\",
        "/Temp/"
    ];

    private static readonly string[] GenericProcessNames = ["update", "service", "host", "win", "system"];

    private static readonly HashSet<string> WindowsProcessAllowList = new(StringComparer.OrdinalIgnoreCase)
    {
        "AggregatorHost",
        "AppProvisioningPlugin",
        "svchost",
        "RuntimeBroker",
        "SearchHost",
        "StartMenuExperienceHost",
        "ShellExperienceHost",
        "SecurityHealthSystray",
        "MsMpEng",
        "explorer",
        "dwm",
        "conhost",
        "csrss",
        "wininit",
        "winlogon",
        "services",
        "lsass",
        "smss",
        "System",
        "Registry"
    };

    private static readonly string[] TrustedPublishers =
    [
        "Microsoft Corporation",
        "Google LLC",
        "Adobe Inc.",
        "Apple Inc.",
        "Mozilla Corporation",
        "GitHub, Inc.",
        "Spotify AB",
        "Discord Inc.",
        "Valve Corp.",
        "NVIDIA Corporation",
        "Intel Corporation",
        "Advanced Micro Devices",
        "Realtek"
    ];

    public IReadOnlyList<RiskFinding> Evaluate(ScanResult scanResult)
    {
        var builder = new FindingBuilder();
        var startupItems = scanResult.StartupItems.ToList();

        foreach (var process in scanResult.Processes)
        {
            EvaluateProcess(process, startupItems, builder);
        }

        foreach (var startupItem in startupItems)
        {
            EvaluateStartupItem(startupItem, builder);
        }

        if (scanResult.StartupItems.Count > 30)
        {
            builder.Add(new RiskFinding
            {
                Level = RiskLevel.Low,
                Category = "Autostart",
                RuleId = "startup.too_many",
                Name = "Viele Autostart-Eintraege",
                Reason = $"{scanResult.StartupItems.Count} Autostart-Eintraege koennen Systemstart und Anmeldung verlangsamen.",
                Recommendation = "Autostart-Liste pruefen und nicht benoetigte Eintraege deaktivieren.",
                SuggestedActions = [FindingAction.OpenFileLocation]
            });
        }

        if (!scanResult.DefenderStatus.IsAvailable || scanResult.DefenderStatus.ErrorMessage is not null)
        {
            builder.Add(new RiskFinding
            {
                Level = RiskLevel.Info,
                Category = "Windows Defender",
                RuleId = "defender.not_readable",
                Name = "Defender-Status nicht lesbar",
                Reason = scanResult.DefenderStatus.ErrorMessage ?? "Der Windows Defender Status konnte nicht gelesen werden.",
                Recommendation = "Windows-Sicherheit oeffnen und Defender-Status manuell pruefen.",
                SuggestedActions = [FindingAction.StartDefenderQuickScan]
            });
        }
        else if (scanResult.DefenderStatus.RealTimeProtectionEnabled == false || scanResult.DefenderStatus.AntivirusEnabled == false)
        {
            builder.Add(new RiskFinding
            {
                Level = RiskLevel.High,
                Category = "Windows Defender",
                RuleId = "defender.disabled",
                Name = "Defender ist deaktiviert",
                Reason = "Windows Defender meldet deaktivierten Virenschutz oder Echtzeitschutz.",
                Recommendation = "Windows-Sicherheit oeffnen und Echtzeitschutz wieder aktivieren.",
                SuggestedActions = [FindingAction.StartDefenderQuickScan]
            });
        }

        return builder.Findings.OrderByDescending(finding => Score(finding.Level)).ThenBy(finding => finding.Category).ToList();
    }

    public RiskLevel GetOverallRiskLevel(IEnumerable<RiskFinding> findings)
    {
        return findings
            .Where(finding => finding.Level is not RiskLevel.Info and not RiskLevel.None)
            .Select(finding => finding.Level)
            .DefaultIfEmpty(RiskLevel.None)
            .OrderByDescending(Score)
            .First();
    }

    private static void EvaluateProcess(ProcessScanItem process, IReadOnlyList<StartupItem> startupItems, FindingBuilder builder)
    {
        var path = process.Path;
        var isUserWritable = !string.IsNullOrWhiteSpace(path) && IsUserWritableLocation(path);
        var isHighRiskWritablePath = !string.IsNullOrWhiteSpace(path) && IsHighRiskWritableLocation(path);
        var isStartup = IsStartupProcess(process, startupItems);
        var isGenericName = GenericProcessNames.Any(name => process.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
        var isNewFile = IsNewFile(path);
        var highCpu = process.CpuUsagePercent >= 80;
        var elevatedCpu = process.CpuUsagePercent >= 60;
        var trusted = IsTrusted(process);
        var allowedWindowsProcess = IsAllowedWindowsProcess(process, isUserWritable);
        var unsigned = process.SignatureTrustStatus == SignatureTrustStatus.Unsigned;

        if (allowedWindowsProcess || trusted)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(path) || process.SignatureTrustStatus is SignatureTrustStatus.Unknown or SignatureTrustStatus.NotAccessible)
        {
            builder.Add(new RiskFinding
            {
                Level = RiskLevel.Info,
                Category = "Prozesse",
                RuleId = "process.not_fully_checked",
                Name = process.Name,
                ProcessId = process.ProcessId,
                TargetPath = path,
                Publisher = process.Publisher,
                Reason = "Prozess wurde nicht vollstaendig geprueft, weil Pfad oder Signatur nicht lesbar war.",
                Recommendation = "Bei Unsicherheit Dateispeicherort oeffnen und Windows Defender Schnellscan starten.",
                SuggestedActions = SafeActions(process, false)
            });
        }

        var signals = new List<string>();
        if (isUserWritable)
        {
            signals.Add("laeuft aus AppData, Temp oder Downloads");
        }

        if (unsigned)
        {
            signals.Add("nicht signiert");
        }

        if (isStartup)
        {
            signals.Add("Autostart-Eintrag vorhanden");
        }

        if (isGenericName)
        {
            signals.Add("generischer oder vertrauensaehnlicher Name");
        }

        if (isNewFile)
        {
            signals.Add("Datei ist sehr neu");
        }

        if (highCpu)
        {
            signals.Add($"sehr hohe CPU-Auslastung ({process.CpuUsagePercent:0.0}%)");
        }
        else if (elevatedCpu)
        {
            signals.Add($"erhoehte CPU-Auslastung ({process.CpuUsagePercent:0.0}%)");
        }

        if (unsigned && isHighRiskWritablePath && isStartup)
        {
            builder.Add(CreateProcessFinding(
                RiskLevel.High,
                "process.unsigned_writable_startup",
                process,
                signals,
                "Nicht signierte Autostart-Datei aus AppData/Temp pruefen. Nur entfernen, wenn sie eindeutig unerwuenscht ist.",
                riskyActions: true));
            return;
        }

        if (unsigned && isGenericName && isUserWritable && signals.Count >= 3)
        {
            builder.Add(CreateProcessFinding(
                RiskLevel.Medium,
                "process.suspicious_unsigned_generic",
                process,
                signals,
                "Datei mit Windows Defender scannen und Herkunft pruefen.",
                riskyActions: false));
            return;
        }

        if (signals.Count >= 3 && unsigned)
        {
            builder.Add(CreateProcessFinding(
                RiskLevel.Medium,
                "process.multiple_unsigned_signals",
                process,
                signals,
                "Mehrere Hinweise zusammen pruefen und Datei mit Defender scannen.",
                riskyActions: false));
        }
        else if ((highCpu && unsigned) || (elevatedCpu && isUserWritable && unsigned))
        {
            builder.Add(CreateProcessFinding(
                RiskLevel.Low,
                "process.cpu_unsigned",
                process,
                signals,
                "CPU-Auslastung beobachten und bei Auffaelligkeit Defender Schnellscan starten.",
                riskyActions: false));
        }
    }

    private static void EvaluateStartupItem(StartupItem startupItem, FindingBuilder builder)
    {
        var userWritable = IsUserWritableLocation(startupItem.Command);
        var trustedPublisher = IsTrustedPublisher(startupItem.Publisher);
        var unknownPublisher = string.IsNullOrWhiteSpace(startupItem.Publisher) || startupItem.Publisher.Equals("Unknown", StringComparison.OrdinalIgnoreCase);
        var genericName = GenericProcessNames.Any(name => startupItem.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

        if (trustedPublisher)
        {
            return;
        }

        if (userWritable && unknownPublisher && genericName)
        {
            builder.Add(new RiskFinding
            {
                Level = RiskLevel.Medium,
                Category = "Autostart",
                RuleId = "startup.writable_unknown_generic",
                Name = startupItem.Name,
                TargetPath = startupItem.Command,
                Publisher = startupItem.Publisher,
                Reason = "Autostart-Eintrag aus Benutzerverzeichnis mit unbekanntem Herausgeber und generischem Namen.",
                Recommendation = "Eintrag pruefen, Defender Schnellscan starten und nur bei klarer Unerwuenschtheit deaktivieren.",
                SuggestedActions =
                [
                    FindingAction.OpenFileLocation,
                    FindingAction.DisableStartupEntry,
                    FindingAction.StartDefenderQuickScan
                ]
            });
        }
        else if (userWritable && unknownPublisher)
        {
            builder.Add(new RiskFinding
            {
                Level = RiskLevel.Low,
                Category = "Autostart",
                RuleId = "startup.writable_unknown",
                Name = startupItem.Name,
                TargetPath = startupItem.Command,
                Publisher = startupItem.Publisher,
                Reason = "Autostart-Eintrag aus Benutzerverzeichnis mit unbekanntem Herausgeber.",
                Recommendation = "Herkunft pruefen. Viele legitime Apps nutzen AppData, daher kein automatisches Entfernen.",
                SuggestedActions = [FindingAction.OpenFileLocation, FindingAction.StartDefenderQuickScan]
            });
        }
    }

    private static RiskFinding CreateProcessFinding(
        RiskLevel level,
        string ruleId,
        ProcessScanItem process,
        IReadOnlyList<string> signals,
        string recommendation,
        bool riskyActions)
    {
        return new RiskFinding
        {
            Level = level,
            Category = "Prozesse",
            RuleId = ruleId,
            Name = process.Name,
            ProcessId = process.ProcessId,
            TargetPath = process.Path,
            Publisher = process.Publisher,
            Reason = signals.Count == 0 ? "Auffaellige Prozessmerkmale erkannt." : string.Join("; ", signals),
            Recommendation = recommendation,
            SuggestedActions = SafeActions(process, riskyActions)
        };
    }

    private static IReadOnlyList<FindingAction> SafeActions(ProcessScanItem process, bool riskyActions)
    {
        if (IsTrusted(process) || IsAllowedWindowsProcess(process, false))
        {
            return [FindingAction.OpenFileLocation, FindingAction.StartDefenderQuickScan];
        }

        return riskyActions
            ?
            [
                FindingAction.OpenFileLocation,
                FindingAction.DisableStartupEntry,
                FindingAction.StopProcess,
                FindingAction.MoveToQuarantine,
                FindingAction.StartDefenderQuickScan
            ]
            : [FindingAction.OpenFileLocation, FindingAction.StartDefenderQuickScan];
    }

    private static bool IsTrusted(ProcessScanItem process)
    {
        return process.SignatureTrustStatus is SignatureTrustStatus.TrustedMicrosoft or SignatureTrustStatus.TrustedVendor
            || process.IsMicrosoftSigned == true
            || IsTrustedPublisher(process.Publisher);
    }

    private static bool IsTrustedPublisher(string? publisher)
    {
        return !string.IsNullOrWhiteSpace(publisher)
            && TrustedPublishers.Any(trusted => publisher.Contains(trusted, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsAllowedWindowsProcess(ProcessScanItem process, bool isUserWritable)
    {
        return !isUserWritable && WindowsProcessAllowList.Contains(process.Name);
    }

    private static bool IsStartupProcess(ProcessScanItem process, IEnumerable<StartupItem> startupItems)
    {
        if (string.IsNullOrWhiteSpace(process.Path))
        {
            return false;
        }

        return startupItems.Any(item =>
            item.Command.Contains(process.Path, StringComparison.OrdinalIgnoreCase)
            || item.Command.Contains(process.Name, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsNewFile(string? path)
    {
        try
        {
            return !string.IsNullOrWhiteSpace(path)
                && File.Exists(path)
                && DateTime.UtcNow - File.GetCreationTimeUtc(path) < TimeSpan.FromDays(7);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsUserWritableLocation(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && UserWritablePathMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsHighRiskWritableLocation(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && HighRiskPathMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static int Score(RiskLevel level)
    {
        return level switch
        {
            RiskLevel.Critical => 5,
            RiskLevel.High => 4,
            RiskLevel.Medium => 3,
            RiskLevel.Low => 2,
            RiskLevel.None => 1,
            RiskLevel.Info => 0,
            _ => 0
        };
    }

    private sealed class FindingBuilder
    {
        private readonly Dictionary<string, RiskFinding> findings = new(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<RiskFinding> Findings => findings.Values;

        public void Add(RiskFinding finding)
        {
            if (string.IsNullOrWhiteSpace(finding.RuleId))
            {
                return;
            }

            findings.TryAdd(finding.Key, finding);
        }
    }
}
