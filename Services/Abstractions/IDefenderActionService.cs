using MyPcGuard.Models;

namespace MyPcGuard.Services.Abstractions;

public interface IDefenderActionService
{
    Task<ActionResult> StartQuickScanAsync(CancellationToken cancellationToken);
    Task<ActionResult> StartFullScanAsync(CancellationToken cancellationToken);
    Task<ActionResult> OpenWindowsSecurityAsync(CancellationToken cancellationToken);
}
