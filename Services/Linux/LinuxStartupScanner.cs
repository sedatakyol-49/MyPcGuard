using System.Diagnostics;
using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Linux;

public sealed class LinuxStartupScanner : IStartupScanner
{
    private static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private static readonly string DesktopAutostartPath = Path.Combine(Home, ".config", "autostart");
    private static readonly string UserSystemdPath = Path.Combine(Home, ".config", "systemd", "user");
    private static readonly string BackupPath = Path.Combine(Home, ".local", "share", "MyPcGuard", "startup-backup.json");

    public Task<IReadOnlyList<StartupItem>> ScanAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            var items = new List<StartupItem>();
            ReadDesktopEntries(items);
            ReadSystemdUserServices(items);
            ReadDisabledBackups(items);
            cancellationToken.ThrowIfCancellationRequested();
            return (IReadOnlyList<StartupItem>)items.OrderBy(item => item.IsEnabled ? 0 : 1).ThenBy(item => item.Name).ToList();
        }, cancellationToken);
    }

    private static void ReadDesktopEntries(ICollection<StartupItem> items)
    {
        if (!Directory.Exists(DesktopAutostartPath))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(DesktopAutostartPath, "*.desktop"))
        {
            try
            {
                var values = ReadDesktopFile(file);
                var command = values.GetValueOrDefault("Exec", string.Empty);
                var name = values.GetValueOrDefault("Name", Path.GetFileNameWithoutExtension(file));
                if (values.TryGetValue("Hidden", out var hidden) && hidden.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                items.Add(BuildItem("Linux", DesktopAutostartPath, Path.GetFileName(file), name, command, file, true));
            }
            catch
            {
            }
        }
    }

    private static void ReadSystemdUserServices(ICollection<StartupItem> items)
    {
        if (!Directory.Exists(UserSystemdPath))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(UserSystemdPath, "*.service"))
        {
            try
            {
                var lines = File.ReadAllLines(file);
                var command = lines.FirstOrDefault(line => line.StartsWith("ExecStart=", StringComparison.OrdinalIgnoreCase))?.Replace("ExecStart=", string.Empty) ?? string.Empty;
                var name = Path.GetFileNameWithoutExtension(file);
                items.Add(BuildItem("LinuxSystemdUser", UserSystemdPath, Path.GetFileName(file), name, command, file, true));
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

            var backups = System.Text.Json.JsonSerializer.Deserialize<List<LinuxStartupBackupEntry>>(File.ReadAllText(BackupPath)) ?? [];
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
        var classification = Classify(name, command, executablePath);
        var essential = classification == StartupClassification.Essential;

        return new StartupItem
        {
            Id = $"{sourceKind}|{filePath}",
            Name = name,
            Command = command,
            ExecutablePath = executablePath,
            Arguments = ExtractArguments(command, executablePath),
            Publisher = "Linux package/user",
            SignatureStatus = "Not applicable",
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

    private static Dictionary<string, string> ReadDesktopFile(string file)
    {
        return File.ReadAllLines(file)
            .Select(line => line.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim(), StringComparer.OrdinalIgnoreCase);
    }

    private static StartupClassification Classify(string name, string command, string executablePath)
    {
        var combined = $"{name} {command}";
        if (combined.Contains("polkit", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("gnome-keyring", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("kdeconnect", StringComparison.OrdinalIgnoreCase))
        {
            return StartupClassification.Recommended;
        }

        if (combined.Contains("update", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("helper", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("tray", StringComparison.OrdinalIgnoreCase))
        {
            return StartupClassification.Unnecessary;
        }

        if (string.IsNullOrWhiteSpace(executablePath)
            || combined.Contains("sh -c", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("curl", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("wget", StringComparison.OrdinalIgnoreCase))
        {
            return StartupClassification.Suspicious;
        }

        return StartupClassification.Optional;
    }

    private static string GetRecommendation(StartupClassification classification)
    {
        return classification switch
        {
            StartupClassification.Recommended => "Aktiv lassen, wenn die Funktion genutzt wird.",
            StartupClassification.Optional => "Optional. Deaktivieren ist meist unkritisch.",
            StartupClassification.Unnecessary => "Kann oft deaktiviert werden, wenn nicht benoetigt.",
            StartupClassification.Suspicious => "Speicherort pruefen und Prozess nur bei Bedarf stoppen.",
            _ => "Zuerst Speicherort pruefen."
        };
    }

    private static string ExtractExecutablePath(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return string.Empty;
        }

        var first = command.Trim().Trim('"').Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        if (first.StartsWith("/"))
        {
            return first;
        }

        return first;
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

    private sealed record LinuxStartupBackupEntry
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
