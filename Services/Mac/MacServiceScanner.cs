using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Mac;

public sealed class MacServiceScanner : IServiceScanner
{
    public Task<IReadOnlyList<ServiceScanItem>> ScanAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<ServiceScanItem>>([]);
    }
}
