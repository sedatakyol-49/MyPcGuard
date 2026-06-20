using MyPcGuard.Models;

namespace MyPcGuard.Services.Abstractions;

public interface ISystemInfoService
{
    Task<SystemOverview> GetOverviewAsync(CancellationToken cancellationToken);
}
