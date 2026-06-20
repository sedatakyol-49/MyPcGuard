using MyPcGuard.Models;

namespace MyPcGuard.Services.Abstractions;

public interface IReportGenerator
{
    Task<string> ExportHtmlAsync(ScanResult scanResult, CancellationToken cancellationToken);
}
