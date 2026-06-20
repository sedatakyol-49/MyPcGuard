using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Common;

public sealed class UnsupportedInstalledProgramActionService : IInstalledProgramActionService
{
    public Task<ActionResult> StartUninstallAsync(InstalledProgramItem item, CancellationToken cancellationToken)
    {
        return Task.FromResult(new ActionResult
        {
            Status = ActionResultStatus.NotSupported,
            Message = "Action_NotSupported"
        });
    }
}
