using MyPcGuard.Agents.Models;

namespace MyPcGuard.Agents.Abstractions;

public interface IDeviceDriverScanner
{
    Task<IReadOnlyList<DriverIssue>> ScanAsync(CancellationToken cancellationToken);
}
