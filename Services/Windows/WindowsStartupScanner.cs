using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.Win32;
using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Windows;

#pragma warning disable CA1416
public sealed class WindowsStartupScanner : IStartupScanner
{
    public const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private static readonly string BackupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyPcGuard", "startup-backup.json");
    private static readonly string[] EssentialPublisherMarkers = ["Microsoft Corporation", "Intel", "Advanced Micro Devices", "AMD", "NVIDIA", "Realtek", "Synaptics", "ELAN"];
    private static readonly string[] OptionalMarkers = ["Spotify", "Discord", "Steam", "Epic Games", "WhatsApp", "Zoom", "Adobe", "Google", "Teams"];
    private static readonly string[] UnnecessaryMarkers = ["updater", "update", "helper", "tray", "launcher"];
    private static readonly string[] SuspiciousNames = ["winupdate", "windowsservice", "systemhost", "svhost", "servicehost"];

    public Task<IReadOnlyList<StartupItem>> ScanAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            var items = new List<StartupItem>();
            ReadRunKey(Registry.CurrentUser, "HKCU", items);
            ReadRunKey(Registry.LocalMachine, "HKLM", items);
            ReadDisabledBackups(items);

            cancellationToken.ThrowIfCancellationRequested();
            return (IReadOnlyList<StartupItem>)items.OrderBy(item => item.IsEnabled ? 0 : 1).ThenBy(item => item.Name).ToList();
        }, cancellationToken);
    }

    private static void ReadRunKey(RegistryKey root, string hive, ICollection<StartupItem> items)
    {
        try
        {
            using var key = root.OpenSubKey(RunKeyPath);
            if (key is null)
            {
                return;
            }

            foreach (var name in key.GetValueNames())
            {
                var command = key.GetValue(name)?.ToString() ?? string.Empty;
                items.Add(BuildItem(hive, RunKeyPath, name, command, true));
            }
        }
        catch
        {
        }
    }

    private static void ReadDisabledBackups(ICollection<StartupItem> items)
    {
        try
        {
            if (!File.Exists(BackupPath))
            {
                return;
            }

            var backups = JsonSerializer.Deserialize<List<StartupBackupEntry>>(File.ReadAllText(BackupPath)) ?? [];
            foreach (var backup in backups.Where(entry => !entry.IsEnabled))
            {
                if (items.Any(item => item.Id.Equals(backup.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                items.Add(BuildItem(backup.RegistryHive, backup.RegistryKeyPath, backup.RegistryValueName, backup.Command, false));
            }
        }
        catch
        {
        }
    }

    private static StartupItem BuildItem(string hive, string keyPath, string valueName, string command, bool enabled)
    {
        var executablePath = ExtractExecutablePath(command) ?? string.Empty;
        var publisher = GetPublisher(executablePath);
        var signature = GetSignature(executablePath);
        var relatedPids = GetRelatedProcessIds(executablePath);
        var classification = Classify(valueName, command, executablePath, publisher, signature.trustStatus);
        var essential = classification == StartupClassification.Essential;

        return new StartupItem
        {
            Id = $"{hive}|{keyPath}|{valueName}",
            Name = valueName,
            Command = command,
            ExecutablePath = executablePath,
            Arguments = ExtractArguments(command, executablePath),
            Publisher = publisher,
            SignatureStatus = signature.status,
            RegistryHive = hive,
            RegistryKeyPath = keyPath,
            RegistryValueName = valueName,
            IsEnabled = enabled,
            IsCurrentlyRunning = relatedPids.Count > 0,
            RelatedProcessIds = relatedPids,
            StartupClassification = classification,
            Recommendation = GetRecommendation(classification),
            CanDisable = enabled && !essential,
            CanEnable = !enabled,
            CanStopProcess = relatedPids.Count > 0 && !essential,
            SafetyMessage = essential ? "Systemkritisch - nicht empfohlen" : GetRecommendation(classification)
        };
    }

    private static StartupClassification Classify(string name, string command, string executablePath, string publisher, SignatureTrustStatus signatureTrustStatus)
    {
        var combined = $"{name} {command} {publisher}";

        if (IsSuspicious(name, command, executablePath, publisher, signatureTrustStatus))
        {
            return StartupClassification.Suspicious;
        }

        if (IsEssential(name, command, executablePath, publisher, signatureTrustStatus))
        {
            return StartupClassification.Essential;
        }

        if (combined.Contains("OneDrive", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("VPN", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("Backup", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("Sync", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("Password", StringComparison.OrdinalIgnoreCase))
        {
            return StartupClassification.Recommended;
        }

        if (OptionalMarkers.Any(marker => combined.Contains(marker, StringComparison.OrdinalIgnoreCase)))
        {
            return StartupClassification.Optional;
        }

        if (UnnecessaryMarkers.Any(marker => combined.Contains(marker, StringComparison.OrdinalIgnoreCase)))
        {
            return StartupClassification.Unnecessary;
        }

        return StartupClassification.Unknown;
    }

    private static bool IsEssential(string name, string command, string executablePath, string publisher, SignatureTrustStatus signatureTrustStatus)
    {
        var combined = $"{name} {command} {publisher}";
        return EssentialPublisherMarkers.Any(marker => publisher.Contains(marker, StringComparison.OrdinalIgnoreCase))
            || combined.Contains("Windows Defender", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("SecurityHealth", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("MsMpEng", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("Windows Security", StringComparison.OrdinalIgnoreCase)
            || (signatureTrustStatus == SignatureTrustStatus.TrustedMicrosoft
                && executablePath.StartsWith(Environment.SystemDirectory, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSuspicious(string name, string command, string executablePath, string publisher, SignatureTrustStatus signatureTrustStatus)
    {
        var unknownPublisher = string.IsNullOrWhiteSpace(publisher) || publisher.Equals("Unknown", StringComparison.OrdinalIgnoreCase);
        var userWritable = IsUserWritable(command);
        var genericName = SuspiciousNames.Any(marker => name.Contains(marker, StringComparison.OrdinalIgnoreCase));
        var scriptHost = command.Contains("powershell", StringComparison.OrdinalIgnoreCase)
            || command.Contains("cmd", StringComparison.OrdinalIgnoreCase)
            || command.Contains("wscript", StringComparison.OrdinalIgnoreCase)
            || command.Contains("rundll32", StringComparison.OrdinalIgnoreCase);

        return string.IsNullOrWhiteSpace(executablePath)
            || genericName
            || scriptHost
            || ((unknownPublisher || signatureTrustStatus == SignatureTrustStatus.Unsigned) && userWritable);
    }

    private static string GetRecommendation(StartupClassification classification)
    {
        return classification switch
        {
            StartupClassification.Essential => "Systemkritisch - nicht empfohlen",
            StartupClassification.Recommended => "Aktiv lassen, wenn die Funktion genutzt wird.",
            StartupClassification.Optional => "Optional. Deaktivieren ist meist unkritisch.",
            StartupClassification.Unnecessary => "Kann oft deaktiviert werden, wenn nicht benoetigt.",
            StartupClassification.Suspicious => "Zuerst Defender Scan starten und Herkunft pruefen.",
            _ => "Zuerst Speicherort und Signatur pruefen."
        };
    }

    private static string? ExtractExecutablePath(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return null;
        }

        var trimmed = command.Trim();
        if (trimmed.StartsWith('"'))
        {
            var end = trimmed.IndexOf('"', 1);
            return end > 1 ? trimmed[1..end] : null;
        }

        var exeIndex = trimmed.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        return exeIndex > 0 ? trimmed[..(exeIndex + 4)] : trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
    }

    private static string ExtractArguments(string command, string executablePath)
    {
        if (string.IsNullOrWhiteSpace(command) || string.IsNullOrWhiteSpace(executablePath))
        {
            return string.Empty;
        }

        return command.Replace($"\"{executablePath}\"", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(executablePath, string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    private static string GetPublisher(string? path)
    {
        try
        {
            return string.IsNullOrWhiteSpace(path) || !File.Exists(path)
                ? "Unknown"
                : FileVersionInfo.GetVersionInfo(path).CompanyName ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static (string status, SignatureTrustStatus trustStatus) GetSignature(string? path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return ("Not accessible", SignatureTrustStatus.NotAccessible);
            }

            if (!File.Exists(path))
            {
                return ("Unknown signature", SignatureTrustStatus.Unknown);
            }

#pragma warning disable SYSLIB0057
            using var certificate = new X509Certificate2(X509Certificate.CreateFromSignedFile(path));
#pragma warning restore SYSLIB0057
            var microsoft = certificate.Subject.Contains("Microsoft", StringComparison.OrdinalIgnoreCase)
                || certificate.Issuer.Contains("Microsoft", StringComparison.OrdinalIgnoreCase);
            return (microsoft ? "Microsoft signed" : "Signed", microsoft ? SignatureTrustStatus.TrustedMicrosoft : SignatureTrustStatus.TrustedVendor);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return ("Unsigned", SignatureTrustStatus.Unsigned);
        }
        catch
        {
            return ("Unknown signature", SignatureTrustStatus.Unknown);
        }
    }

    private static List<int> GetRelatedProcessIds(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return [];
        }

        var processIds = new List<int>();
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    if (process.MainModule?.FileName?.Equals(executablePath, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        processIds.Add(process.Id);
                    }
                }
                catch
                {
                }
            }
        }

        return processIds;
    }

    private static bool IsUserWritable(string value)
    {
        return value.Contains("\\AppData\\", StringComparison.OrdinalIgnoreCase)
            || value.Contains("\\Temp\\", StringComparison.OrdinalIgnoreCase)
            || value.Contains("\\Downloads\\", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record StartupBackupEntry
    {
        public string Id { get; init; } = string.Empty;
        public string RegistryHive { get; init; } = string.Empty;
        public string RegistryKeyPath { get; init; } = string.Empty;
        public string RegistryValueName { get; init; } = string.Empty;
        public string Command { get; init; } = string.Empty;
        public bool IsEnabled { get; set; }
    }
}
#pragma warning restore CA1416
