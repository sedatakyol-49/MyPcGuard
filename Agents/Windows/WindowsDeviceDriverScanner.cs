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
                Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"Get-PnpDevice | Select-Object Status,Class,FriendlyName,InstanceId,Manufacturer,Problem,ProblemDescription | ConvertTo-Json -Depth 2\"",
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

            return ParseDevices(output);
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<DriverIssue> ParseDevices(string json)
    {
        using var document = JsonDocument.Parse(json);
        var items = document.RootElement.ValueKind == JsonValueKind.Array
            ? document.RootElement.EnumerateArray().ToList()
            : [document.RootElement];

        return items.Where(element => element.ValueKind == JsonValueKind.Object)
            .Select(element =>
            {
                var status = ReadString(element, "Status");
                var problemCode = ReadString(element, "Problem");
                var problemDescription = ReadString(element, "ProblemDescription");
                var isProblematic = IsProblemStatus(status, problemCode);

                return new DriverIssue
                {
                    DeviceName = ReadString(element, "FriendlyName"),
                    DeviceClass = ReadString(element, "Class"),
                    Manufacturer = ReadString(element, "Manufacturer"),
                    InstanceId = ReadString(element, "InstanceId"),
                    Status = string.IsNullOrWhiteSpace(status) ? "Unknown" : status,
                    IsProblematic = isProblematic,
                    Reason = BuildReason(status, problemCode, problemDescription, isProblematic),
                    RiskLevel = GetRiskLevel(status, isProblematic)
                };
            })
            .Where(issue => !string.IsNullOrWhiteSpace(issue.DeviceName) || !string.IsNullOrWhiteSpace(issue.InstanceId))
            .ToList();
    }

    private static bool IsProblemStatus(string status, string problemCode)
    {
        if (!string.IsNullOrWhiteSpace(problemCode) && problemCode != "0")
        {
            return true;
        }

        return !status.Equals("OK", StringComparison.OrdinalIgnoreCase);
    }

    private static RiskLevel GetRiskLevel(string status, bool isProblematic)
    {
        if (!isProblematic)
        {
            return RiskLevel.Info;
        }

        return status.Equals("Error", StringComparison.OrdinalIgnoreCase) ? RiskLevel.Medium : RiskLevel.Low;
    }

    private static string BuildReason(string status, string problemCode, string problemDescription, bool isProblematic)
    {
        if (!string.IsNullOrWhiteSpace(problemDescription))
        {
            return problemDescription;
        }

        if (!string.IsNullOrWhiteSpace(problemCode) && problemCode != "0")
        {
            return $"Windows reports device problem code {problemCode}.";
        }

        if (!isProblematic)
        {
            return "Device reports OK.";
        }

        return string.IsNullOrWhiteSpace(status)
            ? "Windows did not provide a clear device status."
            : $"Windows reports device status '{status}'.";
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.ToString()
            : string.Empty;
    }
}
