using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Mac;

public sealed class MacProcessScanner : IProcessScanner
{
    public Task<IReadOnlyList<ProcessScanItem>> ScanAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<ProcessScanItem>>([]);
    }
}
