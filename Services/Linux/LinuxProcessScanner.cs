using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Linux;

public sealed class LinuxProcessScanner : IProcessScanner
{
    public Task<IReadOnlyList<ProcessScanItem>> ScanAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<ProcessScanItem>>([]);
    }
}
