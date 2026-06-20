using System.Diagnostics;
using System.Security;
using System.Text.Json;
using Microsoft.Win32;
using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Windows;

#pragma warning disable CA1416
public sealed class WindowsAutostartActionService : IAutostartActionService
{
    private static readonly string AppDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyPcGuard");
    private static readonly string BackupPath = Path.Combine(AppDataDirectory, "startup-backup.json");
    private static readonly string ActionHistoryPath = Path.Combine(AppDataDirectory, "action-history.json");

    public async Task<StartupActionResult> EnableAsync(StartupItem item, CancellationToken cancellationToken)
    {
        try
        {
            var backups = await ReadBackupsAsync(cancellationToken);
            var backup = backups.FirstOrDefault(entry => entry.Id.Equals(item.Id, StringComparison.OrdinalIgnoreCase));
            if (backup is null)
            {
                return await CompleteAsync("Enable", item, StartupActionResult.Fail("Action_EnableStartup_NoBackup"), cancellationToken);
            }

            using var key = OpenRegistryKey(backup.RegistryHive, backup.RegistryKeyPath, writable: true);
            if (key is null)
            {
                return await CompleteAsync("Enable", item, StartupActionResult.Fail("Action_RegistryOpenFailed"), cancellationToken);
            }

            key.SetValue(backup.RegistryValueName, backup.Command, RegistryValueKind.String);
            backup.IsEnabled = true;
            await WriteBackupsAsync(backups, cancellationToken);

            return await CompleteAsync("Enable", item, StartupActionResult.Ok("Action_ActivateStartup_Success", item with
            {
                IsEnabled = true,
                CanEnable = false,
                CanDisable = item.StartupClassification != StartupClassification.Essential
            }), cancellationToken);
        }
        catch (Exception ex) when (IsAccessDenied(ex))
        {
            return await CompleteAsync("Enable", item, StartupActionResult.Fail("Action_RequiresAdmin"), cancellationToken);
        }
        catch (Exception)
        {
            return await CompleteAsync("Enable", item, StartupActionResult.Fail("Action_ActivateStartup_Failed"), cancellationToken);
        }
    }

    public async Task<StartupActionResult> DisableAsync(StartupItem item, CancellationToken cancellationToken)
    {
        if (item.StartupClassification == StartupClassification.Essential || !item.CanDisable)
        {
            return await CompleteAsync("Disable", item, StartupActionResult.Fail("Action_NotAllowed"), cancellationToken);
        }

        try
        {
            var backups = await ReadBackupsAsync(cancellationToken);
            var existing = backups.FirstOrDefault(entry => entry.Id.Equals(item.Id, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                backups.Add(new StartupBackupEntry
                {
                    Id = item.Id,
                    RegistryHive = item.RegistryHive,
                    RegistryKeyPath = item.RegistryKeyPath,
                    RegistryValueName = item.RegistryValueName,
                    Command = item.Command,
                    IsEnabled = false
                });
            }
            else
            {
                existing.Command = item.Command;
                existing.IsEnabled = false;
            }

            await WriteBackupsAsync(backups, cancellationToken);

            using var key = OpenRegistryKey(item.RegistryHive, item.RegistryKeyPath, writable: true);
            if (key is null)
            {
                return await CompleteAsync("Disable", item, StartupActionResult.Fail("Action_RegistryOpenFailed"), cancellationToken);
            }

            key.DeleteValue(item.RegistryValueName, throwOnMissingValue: false);

            return await CompleteAsync("Disable", item, StartupActionResult.Ok("Action_DeactivateStartup_Success", item with
            {
                IsEnabled = false,
                CanEnable = true,
                CanDisable = false
            }), cancellationToken);
        }
        catch (Exception ex) when (IsAccessDenied(ex))
        {
            return await CompleteAsync("Disable", item, StartupActionResult.Fail("Action_RequiresAdmin"), cancellationToken);
        }
        catch (Exception)
        {
            return await CompleteAsync("Disable", item, StartupActionResult.Fail("Action_DeactivateStartup_Failed"), cancellationToken);
        }
    }

    public async Task<StartupActionResult> StopProcessAsync(StartupItem item, CancellationToken cancellationToken)
    {
        if (!item.CanStopProcess || item.StartupClassification == StartupClassification.Essential)
        {
            return await CompleteAsync("StopProcess", item, StartupActionResult.Fail("Action_NotAllowed"), cancellationToken);
        }

        try
        {
            var processIds = item.RelatedProcessIds.Count > 0 ? item.RelatedProcessIds : FindProcessIdsByPath(item.ExecutablePath);
            if (processIds.Count == 0)
            {
                return await CompleteAsync("StopProcess", item, StartupActionResult.Fail("Action_StopProcess_NotRunning"), cancellationToken);
            }

            foreach (var processId in processIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var process = Process.GetProcessById(processId);
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(cancellationToken);
            }

            return await CompleteAsync("StopProcess", item, StartupActionResult.Ok("Action_StopProcess_Success", item with
            {
                IsCurrentlyRunning = false,
                RelatedProcessIds = [],
                CanStopProcess = false
            }), cancellationToken);
        }
        catch (Exception ex) when (IsAccessDenied(ex))
        {
            return await CompleteAsync("StopProcess", item, StartupActionResult.Fail("Action_StopProcess_AccessDenied"), cancellationToken);
        }
        catch (Exception)
        {
            return await CompleteAsync("StopProcess", item, StartupActionResult.Fail("Action_StopProcess_Failed"), cancellationToken);
        }
    }

    public async Task<StartupActionResult> OpenFileLocationAsync(StartupItem item, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(item.ExecutablePath) || !File.Exists(item.ExecutablePath))
            {
                return await CompleteAsync("OpenFileLocation", item, StartupActionResult.Fail("Action_FileNotFound"), cancellationToken);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{item.ExecutablePath}\"",
                UseShellExecute = true
            });

            return await CompleteAsync("OpenFileLocation", item, StartupActionResult.Ok("Action_OpenLocation_Success"), cancellationToken);
        }
        catch (Exception)
        {
            return await CompleteAsync("OpenFileLocation", item, StartupActionResult.Fail("Action_OpenLocation_Failed"), cancellationToken);
        }
    }

    public async Task<StartupActionResult> StartDefenderQuickScanAsync(StartupItem item, CancellationToken cancellationToken)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell",
                ArgumentList = { "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", "Start-MpScan -ScanType QuickScan" },
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process is null)
            {
                return await CompleteAsync("DefenderQuickScan", item, StartupActionResult.Fail("Action_DefenderScan_Failed"), cancellationToken);
            }

            await process.WaitForExitAsync(cancellationToken);
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(cancellationToken);
                return await CompleteAsync("DefenderQuickScan", item, StartupActionResult.Fail(string.IsNullOrWhiteSpace(error) ? "Action_DefenderScan_Failed" : error.Trim()), cancellationToken);
            }

            return await CompleteAsync("DefenderQuickScan", item, StartupActionResult.Ok("Action_DefenderScan_Success"), cancellationToken);
        }
        catch (Exception)
        {
            return await CompleteAsync("DefenderQuickScan", item, StartupActionResult.Fail("Action_DefenderScan_Failed"), cancellationToken);
        }
    }

    private static RegistryKey? OpenRegistryKey(string hive, string keyPath, bool writable)
    {
        var root = hive.Equals("HKLM", StringComparison.OrdinalIgnoreCase)
            ? Registry.LocalMachine
            : Registry.CurrentUser;
        return root.OpenSubKey(keyPath, writable);
    }

    private static IReadOnlyList<int> FindProcessIdsByPath(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return [];
        }

        var result = new List<int>();
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    if (process.MainModule?.FileName?.Equals(executablePath, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        result.Add(process.Id);
                    }
                }
                catch
                {
                }
            }
        }

        return result;
    }

    private static async Task<StartupActionResult> CompleteAsync(string action, StartupItem item, StartupActionResult result, CancellationToken cancellationToken)
    {
        await AppendHistoryAsync(action, item, result, cancellationToken);
        return result;
    }

    private static async Task<List<StartupBackupEntry>> ReadBackupsAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(BackupPath))
            {
                return [];
            }

            await using var stream = File.OpenRead(BackupPath);
            return await JsonSerializer.DeserializeAsync<List<StartupBackupEntry>>(stream, cancellationToken: cancellationToken) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static async Task WriteBackupsAsync(List<StartupBackupEntry> backups, CancellationToken cancellationToken)
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
            if (File.Exists(ActionHistoryPath))
            {
                await using var read = File.OpenRead(ActionHistoryPath);
                history = await JsonSerializer.DeserializeAsync<List<ActionHistoryEntry>>(read, cancellationToken: cancellationToken) ?? [];
            }

            history.Add(new ActionHistoryEntry
            {
                Action = action,
                TargetName = item.Name,
                TargetPath = item.ExecutablePath,
                Success = result.Success,
                Message = result.Message
            });

            await using var write = File.Create(ActionHistoryPath);
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

    private sealed record StartupBackupEntry
    {
        public string Id { get; init; } = string.Empty;
        public string RegistryHive { get; init; } = string.Empty;
        public string RegistryKeyPath { get; init; } = string.Empty;
        public string RegistryValueName { get; init; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }
}
#pragma warning restore CA1416
