using System.Diagnostics;
using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Windows;

public sealed class WindowsServiceScanner : IServiceScanner
{
    public Task<IReadOnlyList<ServiceScanItem>> ScanAsync(CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            try
            {
                var output = await RunPowerShellAsync("Get-Service | Select-Object Name,DisplayName,Status,StartType | ConvertTo-Csv -NoTypeInformation", cancellationToken);
                return ParseCsv(output);
            }
            catch
            {
                return [];
            }
        }, cancellationToken);
    }

    private static async Task<string> RunPowerShellAsync(string command, CancellationToken cancellationToken)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell",
            ArgumentList = { "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", command },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        if (process is null)
        {
            return string.Empty;
        }

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return output;
    }

    private static IReadOnlyList<ServiceScanItem> ParseCsv(string csv)
    {
        var lines = csv.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries).Skip(1);
        return lines.Select(ParseLine)
            .Where(item => item is not null)
            .Cast<ServiceScanItem>()
            .OrderBy(item => item.DisplayName)
            .ToList();
    }

    private static ServiceScanItem? ParseLine(string line)
    {
        var values = SplitCsv(line);
        return values.Count >= 4
            ? new ServiceScanItem { Name = values[0], DisplayName = values[1], Status = values[2], StartType = values[3] }
            : null;
    }

    private static List<string> SplitCsv(string line)
    {
        var values = new List<string>();
        var current = new List<char>();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
            {
                current.Add('"');
                i++;
            }
            else if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(new string(current.ToArray()));
                current.Clear();
            }
            else
            {
                current.Add(c);
            }
        }

        values.Add(new string(current.ToArray()));
        return values;
    }
}
