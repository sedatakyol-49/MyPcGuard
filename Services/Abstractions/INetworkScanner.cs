using MyPcGuard.Models;

namespace MyPcGuard.Services.Abstractions;

public interface INetworkScanner
{
    Task<IReadOnlyList<NetworkConnectionItem>> ScanAsync(CancellationToken cancellationToken);
}
