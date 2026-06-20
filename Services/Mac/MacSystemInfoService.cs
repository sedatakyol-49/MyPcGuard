using System.Runtime.InteropServices;
using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Mac;

public sealed class MacSystemInfoService : ISystemInfoService
{
    public Task<SystemOverview> GetOverviewAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new SystemOverview
        {
            OperatingSystemType = OperatingSystemType.MacOS,
            OperatingSystemName = RuntimeInformation.OSDescription
        });
    }
}
