using System.Diagnostics;
using System.Text.Json;
using MyPcGuard.Agents.Abstractions;
using MyPcGuard.Agents.Models;
using MyPcGuard.Models;

namespace MyPcGuard.Agents.Windows;

public sealed class WindowsDeviceDriverScanner : IDeviceDriverScanner
{
    public async Task<IReadOnlyList<DriverIssue>> ScanAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"Get-PnpDevice | Where-Object { $_.Status -ne 'OK' } | Select-Object Status,Class,FriendlyName,InstanceId,Manufacturer | ConvertTo-Json -Depth 2\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return [];
            }

            var outputTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);
            var output = await outputTask;
            if (string.IsNullOrWhiteSpace(output))
            {
                return [];
            }

            return ParseIssues(output);
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<DriverIssue> ParseIssues(string json)
    {
        using var document = JsonDocument.Parse(json);
        var items = document.RootElement.ValueKind == JsonValueKind.Array
            ? document.RootElement.EnumerateArray().ToList()
            : [document.RootElement];

        return items.Select(element => new DriverIssue
            {
                DeviceName = ReadString(element, "FriendlyName"),
                DeviceClass = ReadString(element, "Class"),
                Manufacturer = ReadString(element, "Manufacturer"),
                InstanceId = ReadString(element, "InstanceId"),
                Status = ReadString(element, "Status"),
                RiskLevel = ReadString(element, "Status").Equals("Error", StringComparison.OrdinalIgnoreCase) ? RiskLevel.Medium : RiskLevel.Low
            })
            .Where(issue => !string.IsNullOrWhiteSpace(issue.DeviceName) || !string.IsNullOrWhiteSpace(issue.InstanceId))
            .ToList();
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.ToString()
            : string.Empty;
    }
}
