using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Linux;

public sealed class LinuxServiceScanner : IServiceScanner
{
    public Task<IReadOnlyList<ServiceScanItem>> ScanAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<ServiceScanItem>>([]);
    }
}
