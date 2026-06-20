using System.Diagnostics;
using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Windows;

public sealed class WindowsDefenderScanner : IDefenderScanner
{
    public async Task<DefenderStatus> ScanAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell",
                ArgumentList =
                {
                    "-NoProfile",
                    "-ExecutionPolicy",
                    "Bypass",
                    "-Command",
                    "Get-MpComputerStatus | Select-Object AMServiceEnabled,AntivirusEnabled,RealTimeProtectionEnabled | ConvertTo-Json -Compress"
                },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process is null)
            {
                return NotAvailable("PowerShell konnte nicht gestartet werden.");
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
            {
                return NotAvailable(string.IsNullOrWhiteSpace(error) ? "Get-MpComputerStatus lieferte keine Daten." : error.Trim());
            }

            var antivirusEnabled = ExtractBool(output, "AntivirusEnabled");
            var realtimeEnabled = ExtractBool(output, "RealTimeProtectionEnabled");

            return new DefenderStatus
            {
                IsAvailable = true,
                AntivirusEnabled = antivirusEnabled,
                RealTimeProtectionEnabled = realtimeEnabled,
                StatusText = realtimeEnabled == true && antivirusEnabled == true ? "Aktiv" : "Nicht vollständig aktiv"
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return NotAvailable(ex.Message);
        }
    }

    private static DefenderStatus NotAvailable(string message)
    {
        return new DefenderStatus
        {
            IsAvailable = false,
            StatusText = "Nicht lesbar",
            ErrorMessage = message
        };
    }

    private static bool? ExtractBool(string json, string propertyName)
    {
        var marker = $"\"{propertyName}\":";
        var index = json.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return null;
        }

        var valueStart = index + marker.Length;
        if (json.AsSpan(valueStart).StartsWith("true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (json.AsSpan(valueStart).StartsWith("false", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return null;
    }
}
