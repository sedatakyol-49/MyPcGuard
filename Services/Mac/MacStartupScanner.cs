using System.Diagnostics;
using System.Xml.Linq;
using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Mac;

public sealed class MacStartupScanner : IStartupScanner
{
    private static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private static readonly string UserLaunchAgentsPath = Path.Combine(Home, "Library", "LaunchAgents");
    private const string SystemLaunchAgentsPath = "/Library/LaunchAgents";
    private static readonly string BackupPath = Path.Combine(Home, "Library", "Application Support", "MyPcGuard", "startup-backup.json");

    public Task<IReadOnlyList<StartupItem>> ScanAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            var items = new List<StartupItem>();
            ReadLaunchAgents(UserLaunchAgentsPath, "macOSUserLaunchAgents", items);
            ReadLaunchAgents(SystemLaunchAgentsPath, "macOSSystemLaunchAgents", items);
            ReadDisabledBackups(items);
            cancellationToken.ThrowIfCancellationRequested();
            return (IReadOnlyList<StartupItem>)items.OrderBy(item => item.IsEnabled ? 0 : 1).ThenBy(item => item.Name).ToList();
        }, cancellationToken);
    }

    private static void ReadLaunchAgents(string directory, string sourceKind, ICollection<StartupItem> items)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*.plist"))
        {
            try
            {
                var (label, command) = ReadPlist(file);
                items.Add(BuildItem(sourceKind, directory, Path.GetFileName(file), string.IsNullOrWhiteSpace(label) ? Path.GetFileNameWithoutExtension(file) : label, command, file, true));
            }
            catch
            {
            }
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

            var backups = System.Text.Json.JsonSerializer.Deserialize<List<MacStartupBackupEntry>>(File.ReadAllText(BackupPath)) ?? [];
            foreach (var backup in backups.Where(entry => !entry.IsEnabled))
            {
                if (items.Any(item => item.Id.Equals(backup.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                items.Add(BuildItem(backup.SourceKind, Path.GetDirectoryName(backup.OriginalPath) ?? string.Empty, Path.GetFileName(backup.OriginalPath), backup.Name, backup.Command, backup.OriginalPath, false));
            }
        }
        catch
        {
        }
    }

    private static StartupItem BuildItem(string sourceKind, string sourcePath, string sourceName, string name, string command, string filePath, bool enabled)
    {
        var executablePath = ExtractExecutablePath(command);
        var relatedPids = GetRelatedProcessIds(executablePath);
        var classification = Classify(name, command, executablePath, sourceKind);
        var essential = classification == StartupClassification.Essential;

        return new StartupItem
        {
            Id = $"{sourceKind}|{filePath}",
            Name = name,
            Command = command,
            ExecutablePath = executablePath,
            Arguments = ExtractArguments(command, executablePath),
            Publisher = "macOS LaunchAgent",
            SignatureStatus = "Not checked",
            RegistryHive = sourceKind,
            RegistryKeyPath = sourcePath,
            RegistryValueName = sourceName,
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

    private static (string label, string command) ReadPlist(string file)
    {
        var doc = XDocument.Load(file);
        var dict = doc.Descendants("dict").FirstOrDefault();
        if (dict is null)
        {
            return (Path.GetFileNameWithoutExtension(file), string.Empty);
        }

        var elements = dict.Elements().ToList();
        string? label = null;
        string? program = null;
        var args = new List<string>();

        for (var i = 0; i < elements.Count; i++)
        {
            if (elements[i].Name.LocalName != "key")
            {
                continue;
            }

            var key = elements[i].Value;
            var value = i + 1 < elements.Count ? elements[i + 1] : null;
            if (key == "Label")
            {
                label = value?.Value;
            }
            else if (key == "Program")
            {
                program = value?.Value;
            }
            else if (key == "ProgramArguments" && value?.Name.LocalName == "array")
            {
                args = value.Elements("string").Select(element => element.Value).ToList();
            }
        }

        return (label ?? Path.GetFileNameWithoutExtension(file), program ?? string.Join(' ', args));
    }

    private static StartupClassification Classify(string name, string command, string executablePath, string sourceKind)
    {
        var combined = $"{name} {command}";
        if (sourceKind.Contains("System", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("com.apple", StringComparison.OrdinalIgnoreCase))
        {
            return StartupClassification.Essential;
        }

        if (combined.Contains("Adobe", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("Google", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("Spotify", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("Discord", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("Steam", StringComparison.OrdinalIgnoreCase))
        {
            return StartupClassification.Optional;
        }

        if (combined.Contains("updater", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("helper", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("launcher", StringComparison.OrdinalIgnoreCase))
        {
            return StartupClassification.Unnecessary;
        }

        if (string.IsNullOrWhiteSpace(executablePath)
            || combined.Contains("curl", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("bash -c", StringComparison.OrdinalIgnoreCase))
        {
            return StartupClassification.Suspicious;
        }

        return StartupClassification.Unknown;
    }

    private static string GetRecommendation(StartupClassification classification)
    {
        return classification switch
        {
            StartupClassification.Essential => "Systemkritisch - nicht empfohlen",
            StartupClassification.Optional => "Optional. Deaktivieren ist meist unkritisch.",
            StartupClassification.Unnecessary => "Kann oft deaktiviert werden, wenn nicht benoetigt.",
            StartupClassification.Suspicious => "LaunchAgent pruefen und Speicherort oeffnen.",
            _ => "Zuerst Speicherort pruefen."
        };
    }

    private static string ExtractExecutablePath(string command)
    {
        var first = command.Trim().Trim('"').Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        return first.StartsWith("/") ? first : string.Empty;
    }

    private static string ExtractArguments(string command, string executablePath)
    {
        return string.IsNullOrWhiteSpace(executablePath)
            ? string.Empty
            : command.Replace(executablePath, string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
    }

    private static List<int> GetRelatedProcessIds(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return [];
        }

        try
        {
            return Process.GetProcesses()
                .Where(process =>
                {
                    using (process)
                    {
                        try
                        {
                            return process.ProcessName.Equals(Path.GetFileNameWithoutExtension(executablePath), StringComparison.OrdinalIgnoreCase);
                        }
                        catch
                        {
                            return false;
                        }
                    }
                })
                .Select(process => process.Id)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private sealed record MacStartupBackupEntry
    {
        public string Id { get; init; } = string.Empty;
        public string SourceKind { get; init; } = string.Empty;
        public string OriginalPath { get; init; } = string.Empty;
        public string BackupPath { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Command { get; init; } = string.Empty;
        public bool IsEnabled { get; init; }
    }
}
