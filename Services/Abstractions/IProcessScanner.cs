using MyPcGuard.Models;

namespace MyPcGuard.Services.Abstractions;

public interface IProcessScanner
{
    Task<IReadOnlyList<ProcessScanItem>> ScanAsync(CancellationToken cancellationToken);
}
