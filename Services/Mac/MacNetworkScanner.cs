using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Mac;

public sealed class MacNetworkScanner : INetworkScanner
{
    public Task<IReadOnlyList<NetworkConnectionItem>> ScanAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<NetworkConnectionItem>>([]);
    }
}
