using MyPcGuard.Models;

namespace MyPcGuard.Services.Abstractions;

public interface IStartupScanner
{
    Task<IReadOnlyList<StartupItem>> ScanAsync(CancellationToken cancellationToken);
}
