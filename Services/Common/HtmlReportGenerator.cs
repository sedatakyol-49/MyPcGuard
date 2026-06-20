using System.Net;
using System.Text;
using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Common;

public sealed class HtmlReportGenerator(ILocalizationService localizationService) : IReportGenerator
{
    public async Task<string> ExportHtmlAsync(ScanResult scanResult, CancellationToken cancellationToken)
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrWhiteSpace(documents))
        {
            documents = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        Directory.CreateDirectory(documents);

        var path = Path.Combine(documents, $"MyPcGuard_Report_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        await File.WriteAllTextAsync(path, BuildHtml(scanResult), Encoding.UTF8, cancellationToken);
        return path;
    }

    private string BuildHtml(ScanResult result)
    {
        var html = new StringBuilder();
        html.AppendLine("<!doctype html><html lang=\"de\"><head><meta charset=\"utf-8\"><title>MyPcGuard Report</title>");
        html.AppendLine("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:32px;color:#1f2937}h1,h2{color:#111827}.card{border:1px solid #d1d5db;border-radius:8px;padding:16px;margin:12px 0}table{border-collapse:collapse;width:100%;margin:12px 0}th,td{border-bottom:1px solid #e5e7eb;text-align:left;padding:8px}th{background:#f3f4f6}.High,.Critical{color:#b91c1c}.Medium{color:#b45309}.Low{color:#2563eb}</style>");
        html.AppendLine("</head><body>");
        html.AppendLine("<h1>MyPcGuard Report</h1>");
        html.AppendLine($"<div class=\"card\"><strong>Datum:</strong> {WebUtility.HtmlEncode(result.ScannedAt.LocalDateTime.ToString("g"))}<br>");
        html.AppendLine($"<strong>OS:</strong> {Encode(result.Overview.OperatingSystemName)}<br>");
        html.AppendLine($"<strong>Risiko:</strong> <span class=\"{result.OverallRiskLevel}\">{result.OverallRiskLevel}</span><br>");
        html.AppendLine($"<strong>Findings:</strong> {result.Findings.Count}</div>");
        html.AppendLine($"<div class=\"card\"><strong>Disclaimer:</strong> {Encode(localizationService.GetString("Disclaimer_NotAntivirus"))}</div>");

        AppendFindings(html, result.Findings);
        AppendProcesses(html, result.Processes.Take(25));
        AppendStartup(html, result.StartupItems);

        html.AppendLine("<h2>Windows Defender</h2>");
        html.AppendLine("<div class=\"card\">");
        html.AppendLine($"<strong>Status:</strong> {Encode(result.DefenderStatus.StatusText)}<br>");
        html.AppendLine($"<strong>Echtzeitschutz:</strong> {Encode(ToDisplay(result.DefenderStatus.RealTimeProtectionEnabled))}<br>");
        html.AppendLine($"<strong>Virenschutz:</strong> {Encode(ToDisplay(result.DefenderStatus.AntivirusEnabled))}<br>");
        if (!string.IsNullOrWhiteSpace(result.DefenderStatus.ErrorMessage))
        {
            html.AppendLine($"<strong>Fehler:</strong> {Encode(result.DefenderStatus.ErrorMessage)}");
        }

        html.AppendLine("</div></body></html>");
        return html.ToString();
    }

    private static void AppendFindings(StringBuilder html, IEnumerable<RiskFinding> findings)
    {
        html.AppendLine("<h2>Findings</h2><table><tr><th>Risiko</th><th>Kategorie</th><th>Titel</th><th>Beschreibung</th></tr>");
        foreach (var finding in findings)
        {
            html.AppendLine($"<tr><td class=\"{finding.Level}\">{finding.Level}</td><td>{Encode(finding.Category)}</td><td>{Encode(finding.Title)}</td><td>{Encode(finding.Description)}</td></tr>");
        }

        html.AppendLine("</table>");
    }

    private static void AppendProcesses(StringBuilder html, IEnumerable<ProcessScanItem> processes)
    {
        html.AppendLine("<h2>Top Prozesse</h2><table><tr><th>Name</th><th>PID</th><th>CPU</th><th>RAM</th><th>Pfad</th></tr>");
        foreach (var process in processes.OrderByDescending(process => process.CpuUsagePercent))
        {
            html.AppendLine($"<tr><td>{Encode(process.Name)}</td><td>{process.ProcessId}</td><td>{process.CpuUsagePercent:0.0}%</td><td>{FormatBytes(process.MemoryBytes)}</td><td>{Encode(process.Path)}</td></tr>");
        }

        html.AppendLine("</table>");
    }

    private static void AppendStartup(StringBuilder html, IEnumerable<StartupItem> startupItems)
    {
        html.AppendLine("<h2>Autostart</h2><table><tr><th>Name</th><th>Ort</th><th>Herausgeber</th><th>Befehl</th></tr>");
        foreach (var item in startupItems)
        {
            html.AppendLine($"<tr><td>{Encode(item.Name)}</td><td>{Encode(item.Location)}</td><td>{Encode(item.Publisher)}</td><td>{Encode(item.Command)}</td></tr>");
        }

        html.AppendLine("</table>");
    }

    private static string Encode(string? value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }

    private static string ToDisplay(bool? value)
    {
        return value switch
        {
            true => "Aktiv",
            false => "Inaktiv",
            _ => "Unbekannt"
        };
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
        {
            return "0 MB";
        }

        return $"{bytes / 1024d / 1024d:0.0} MB";
    }
}
