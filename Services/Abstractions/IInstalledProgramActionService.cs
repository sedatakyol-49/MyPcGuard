using MyPcGuard.Models;

namespace MyPcGuard.Services.Abstractions;

public interface IInstalledProgramActionService
{
    Task<ActionResult> StartUninstallAsync(InstalledProgramItem item, CancellationToken cancellationToken);
}
