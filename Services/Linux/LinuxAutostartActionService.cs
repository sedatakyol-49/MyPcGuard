using System.Diagnostics;
using System.Security;
using System.Text.Json;
using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Linux;

public sealed class LinuxAutostartActionService : IAutostartActionService
{
    private static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private static readonly string AppDataDirectory = Path.Combine(Home, ".local", "share", "MyPcGuard");
    private static readonly string DisabledDirectory = Path.Combine(AppDataDirectory, "startup-disabled");
    private static readonly string BackupPath = Path.Combine(AppDataDirectory, "startup-backup.json");
    private static readonly string HistoryPath = Path.Combine(AppDataDirectory, "action-history.json");

    public async Task<StartupActionResult> EnableAsync(StartupItem item, CancellationToken cancellationToken)
    {
        try
        {
            var backups = await ReadBackupsAsync(cancellationToken);
            var backup = backups.FirstOrDefault(entry => entry.Id.Equals(item.Id, StringComparison.OrdinalIgnoreCase));
            if (backup is null || !File.Exists(backup.BackupPath))
            {
                return await CompleteAsync("Enable", item, StartupActionResult.Fail("Kein Backup fuer diesen Autostart-Eintrag gefunden."), cancellationToken);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(backup.OriginalPath)!);
            File.Move(backup.BackupPath, backup.OriginalPath, overwrite: true);
            backup.IsEnabled = true;
            await WriteBackupsAsync(backups, cancellationToken);
            return await CompleteAsync("Enable", item, StartupActionResult.Ok("Autostart-Eintrag wurde aktiviert.", item with { IsEnabled = true, CanEnable = false, CanDisable = item.StartupClassification != StartupClassification.Essential }), cancellationToken);
        }
        catch (Exception ex) when (IsAccessDenied(ex))
        {
            return await CompleteAsync("Enable", item, StartupActionResult.Fail("Fuer diese Aktion sind Administratorrechte erforderlich."), cancellationToken);
        }
        catch (Exception ex)
        {
            return await CompleteAsync("Enable", item, StartupActionResult.Fail($"Aktivieren fehlgeschlagen: {ex.Message}"), cancellationToken);
        }
    }

    public async Task<StartupActionResult> DisableAsync(StartupItem item, CancellationToken cancellationToken)
    {
        if (!item.CanDisable || item.StartupClassification == StartupClassification.Essential)
        {
            return await CompleteAsync("Disable", item, StartupActionResult.Fail("Systemkritische Autostart-Eintraege koennen nicht deaktiviert werden."), cancellationToken);
        }

        try
        {
            var originalPath = Path.Combine(item.RegistryKeyPath, item.RegistryValueName);
            if (!File.Exists(originalPath))
            {
                return await CompleteAsync("Disable", item, StartupActionResult.Fail("Autostart-Datei wurde nicht gefunden."), cancellationToken);
            }

            Directory.CreateDirectory(DisabledDirectory);
            var backupFile = Path.Combine(DisabledDirectory, Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(item.Id)).Replace('/', '_') + Path.GetExtension(originalPath));
            File.Move(originalPath, backupFile, overwrite: true);

            var backups = await ReadBackupsAsync(cancellationToken);
            var existing = backups.FirstOrDefault(entry => entry.Id.Equals(item.Id, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                backups.Add(new LinuxStartupBackupEntry
                {
                    Id = item.Id,
                    SourceKind = item.RegistryHive,
                    OriginalPath = originalPath,
                    BackupPath = backupFile,
                    Name = item.Name,
                    Command = item.Command,
                    IsEnabled = false
                });
            }
            else
            {
                existing.BackupPath = backupFile;
                existing.IsEnabled = false;
            }

            await WriteBackupsAsync(backups, cancellationToken);
            return await CompleteAsync("Disable", item, StartupActionResult.Ok("Autostart-Eintrag wurde deaktiviert. Scan erneut starten empfohlen.", item with { IsEnabled = false, CanEnable = true, CanDisable = false }), cancellationToken);
        }
        catch (Exception ex) when (IsAccessDenied(ex))
        {
            return await CompleteAsync("Disable", item, StartupActionResult.Fail("Fuer diese Aktion sind Administratorrechte erforderlich."), cancellationToken);
        }
        catch (Exception ex)
        {
            return await CompleteAsync("Disable", item, StartupActionResult.Fail($"Deaktivieren fehlgeschlagen: {ex.Message}"), cancellationToken);
        }
    }

    public async Task<StartupActionResult> StopProcessAsync(StartupItem item, CancellationToken cancellationToken)
    {
        if (!item.CanStopProcess || item.StartupClassification == StartupClassification.Essential)
        {
            return await CompleteAsync("StopProcess", item, StartupActionResult.Fail("Dieser Prozess kann nicht ueber MyPcGuard gestoppt werden."), cancellationToken);
        }

        try
        {
            foreach (var pid in item.RelatedProcessIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var process = Process.GetProcessById(pid);
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(cancellationToken);
            }

            return await CompleteAsync("StopProcess", item, StartupActionResult.Ok("Zugehoerige Prozesse wurden gestoppt.", item with { IsCurrentlyRunning = false, RelatedProcessIds = [], CanStopProcess = false }), cancellationToken);
        }
        catch (Exception ex) when (IsAccessDenied(ex))
        {
            return await CompleteAsync("StopProcess", item, StartupActionResult.Fail("Zugriff verweigert. Der Prozess kann nicht gestoppt werden."), cancellationToken);
        }
        catch (Exception ex)
        {
            return await CompleteAsync("StopProcess", item, StartupActionResult.Fail($"Prozess stoppen fehlgeschlagen: {ex.Message}"), cancellationToken);
        }
    }

    public async Task<StartupActionResult> OpenFileLocationAsync(StartupItem item, CancellationToken cancellationToken)
    {
        try
        {
            var path = File.Exists(item.ExecutablePath) ? item.ExecutablePath : Path.Combine(item.RegistryKeyPath, item.RegistryValueName);
            var folder = File.Exists(path) ? Path.GetDirectoryName(path) : item.RegistryKeyPath;
            Process.Start(new ProcessStartInfo { FileName = "xdg-open", ArgumentList = { folder ?? Home }, UseShellExecute = false });
            return await CompleteAsync("OpenFileLocation", item, StartupActionResult.Ok("Dateimanager wurde geoeffnet."), cancellationToken);
        }
        catch (Exception ex)
        {
            return await CompleteAsync("OpenFileLocation", item, StartupActionResult.Fail($"Dateimanager konnte nicht geoeffnet werden: {ex.Message}"), cancellationToken);
        }
    }

    public async Task<StartupActionResult> StartDefenderQuickScanAsync(StartupItem item, CancellationToken cancellationToken)
    {
        return await CompleteAsync("DefenderQuickScan", item, StartupActionResult.Fail("Windows Defender Schnellscan ist nur unter Windows verfuegbar."), cancellationToken);
    }

    private static async Task<StartupActionResult> CompleteAsync(string action, StartupItem item, StartupActionResult result, CancellationToken cancellationToken)
    {
        await AppendHistoryAsync(action, item, result, cancellationToken);
        return result;
    }

    private static async Task<List<LinuxStartupBackupEntry>> ReadBackupsAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(BackupPath))
            {
                return [];
            }

            await using var stream = File.OpenRead(BackupPath);
            return await JsonSerializer.DeserializeAsync<List<LinuxStartupBackupEntry>>(stream, cancellationToken: cancellationToken) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static async Task WriteBackupsAsync(List<LinuxStartupBackupEntry> backups, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(AppDataDirectory);
        await using var stream = File.Create(BackupPath);
        await JsonSerializer.SerializeAsync(stream, backups, new JsonSerializerOptions { WriteIndented = true }, cancellationToken);
    }

    private static async Task AppendHistoryAsync(string action, StartupItem item, StartupActionResult result, CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(AppDataDirectory);
            var history = new List<ActionHistoryEntry>();
            if (File.Exists(HistoryPath))
            {
                await using var read = File.OpenRead(HistoryPath);
                history = await JsonSerializer.DeserializeAsync<List<ActionHistoryEntry>>(read, cancellationToken: cancellationToken) ?? [];
            }

            history.Add(new ActionHistoryEntry { Action = action, TargetName = item.Name, TargetPath = item.ExecutablePath, Success = result.Success, Message = result.Message });
            await using var write = File.Create(HistoryPath);
            await JsonSerializer.SerializeAsync(write, history, new JsonSerializerOptions { WriteIndented = true }, cancellationToken);
        }
        catch
        {
        }
    }

    private static bool IsAccessDenied(Exception ex)
    {
        return ex is UnauthorizedAccessException or SecurityException;
    }

    private sealed record LinuxStartupBackupEntry
    {
        public string Id { get; init; } = string.Empty;
        public string SourceKind { get; init; } = string.Empty;
        public string OriginalPath { get; init; } = string.Empty;
        public string BackupPath { get; set; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Command { get; init; } = string.Empty;
        public bool IsEnabled { get; set; }
    }
}
