using System.Runtime.InteropServices;
using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Linux;

public sealed class LinuxSystemInfoService : ISystemInfoService
{
    public Task<SystemOverview> GetOverviewAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new SystemOverview
        {
            OperatingSystemType = OperatingSystemType.Linux,
            OperatingSystemName = RuntimeInformation.OSDescription
        });
    }
}
