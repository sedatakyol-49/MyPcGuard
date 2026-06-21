using MyPcGuard.Agents.Abstractions;
using MyPcGuard.Agents.Models;

namespace MyPcGuard.Agents.Core;

public sealed class UnsupportedDeviceDriverScanner : IDeviceDriverScanner
{
    public Task<IReadOnlyList<DriverIssue>> ScanAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<DriverIssue>>([]);
    }
}
