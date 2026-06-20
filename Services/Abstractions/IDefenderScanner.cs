using MyPcGuard.Models;

namespace MyPcGuard.Services.Abstractions;

public interface IDefenderScanner
{
    Task<DefenderStatus> ScanAsync(CancellationToken cancellationToken);
}
