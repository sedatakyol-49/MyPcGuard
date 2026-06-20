using Microsoft.Win32;
using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Windows;

#pragma warning disable CA1416
public sealed class WindowsInstalledProgramScanner : IInstalledProgramScanner
{
    private const string UninstallKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
    private const string WowUninstallKeyPath = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

    public Task<IReadOnlyList<InstalledProgramItem>> ScanAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            var items = new Dictionary<string, InstalledProgramItem>(StringComparer.OrdinalIgnoreCase);
            ReadPrograms(Registry.LocalMachine, UninstallKeyPath, items, cancellationToken);
            ReadPrograms(Registry.LocalMachine, WowUninstallKeyPath, items, cancellationToken);
            ReadPrograms(Registry.CurrentUser, UninstallKeyPath, items, cancellationToken);

            return (IReadOnlyList<InstalledProgramItem>)items.Values
                .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }, cancellationToken);
    }

    private static void ReadPrograms(RegistryKey root, string path, Dictionary<string, InstalledProgramItem> items, CancellationToken cancellationToken)
    {
        using var key = root.OpenSubKey(path);
        if (key is null)
        {
            return;
        }

        foreach (var subKeyName in key.GetSubKeyNames())
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var subKey = key.OpenSubKey(subKeyName);
            if (subKey is null || IsHiddenSystemEntry(subKey))
            {
                continue;
            }

            var name = ReadString(subKey, "DisplayName");
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var item = new InstalledProgramItem
            {
                Id = $"{root.Name}\\{path}\\{subKeyName}",
                Name = name,
                Version = ReadString(subKey, "DisplayVersion"),
                Publisher = ReadString(subKey, "Publisher"),
                InstallDate = FormatInstallDate(ReadString(subKey, "InstallDate")),
                InstallLocation = ReadString(subKey, "InstallLocation"),
                EstimatedSizeBytes = ReadSizeBytes(subKey),
                UninstallCommand = ReadString(subKey, "UninstallString"),
                QuietUninstallCommand = ReadString(subKey, "QuietUninstallString")
            };

            var keyId = $"{item.Name}|{item.Publisher}|{item.Version}";
            items.TryAdd(keyId, item);
        }
    }

    private static bool IsHiddenSystemEntry(RegistryKey key)
    {
        return ReadInt(key, "SystemComponent") == 1
            || !string.IsNullOrWhiteSpace(ReadString(key, "ParentKeyName"))
            || ReadString(key, "ReleaseType").Contains("Update", StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadString(RegistryKey key, string name)
    {
        try
        {
            return key.GetValue(name)?.ToString()?.Trim() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static int ReadInt(RegistryKey key, string name)
    {
        try
        {
            return key.GetValue(name) is int value ? value : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static long ReadSizeBytes(RegistryKey key)
    {
        try
        {
            return key.GetValue("EstimatedSize") is int sizeKb && sizeKb > 0 ? sizeKb * 1024L : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static string FormatInstallDate(string value)
    {
        return value.Length == 8
            && DateTime.TryParseExact(value, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var date)
            ? date.ToShortDateString()
            : value;
    }
}
#pragma warning restore CA1416
