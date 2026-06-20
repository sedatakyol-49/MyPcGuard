using MyPcGuard.Models;

namespace MyPcGuard.Services.Abstractions;

public interface IInstalledProgramScanner
{
    Task<IReadOnlyList<InstalledProgramItem>> ScanAsync(CancellationToken cancellationToken);
}
