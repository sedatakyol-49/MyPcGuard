using MyPcGuard.Models;

namespace MyPcGuard.Services.Abstractions;

public interface IServiceScanner
{
    Task<IReadOnlyList<ServiceScanItem>> ScanAsync(CancellationToken cancellationToken);
}
