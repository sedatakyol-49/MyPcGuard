using MyPcGuard.Models;

namespace MyPcGuard.Services.Abstractions;

public interface IScanOrchestrator
{
    Task<ScanResult> RunScanAsync(CancellationToken cancellationToken);
}
