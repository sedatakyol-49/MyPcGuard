using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Common;

public sealed class UnsupportedDefenderActionService : IDefenderActionService
{
    public Task<ActionResult> StartQuickScanAsync(CancellationToken cancellationToken) => NotSupported();
    public Task<ActionResult> StartFullScanAsync(CancellationToken cancellationToken) => NotSupported();
    public Task<ActionResult> OpenWindowsSecurityAsync(CancellationToken cancellationToken) => NotSupported();

    private static Task<ActionResult> NotSupported()
    {
        return Task.FromResult(new ActionResult { Status = ActionResultStatus.NotSupported, Message = "Action_NotSupported" });
    }
}
