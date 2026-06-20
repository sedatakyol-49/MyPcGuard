using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Common;

public sealed class UnsupportedInstalledProgramScanner : IInstalledProgramScanner
{
    public Task<IReadOnlyList<InstalledProgramItem>> ScanAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<InstalledProgramItem>>([]);
    }
}
