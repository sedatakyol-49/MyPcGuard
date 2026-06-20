using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Linux;

public sealed class LinuxNetworkScanner : INetworkScanner
{
    public Task<IReadOnlyList<NetworkConnectionItem>> ScanAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<NetworkConnectionItem>>([]);
    }
}
