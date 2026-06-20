using MyPcGuard.Models;

namespace MyPcGuard.Services.Abstractions;

public interface IAutostartActionService
{
    Task<StartupActionResult> EnableAsync(StartupItem item, CancellationToken cancellationToken);
    Task<StartupActionResult> DisableAsync(StartupItem item, CancellationToken cancellationToken);
    Task<StartupActionResult> StopProcessAsync(StartupItem item, CancellationToken cancellationToken);
    Task<StartupActionResult> OpenFileLocationAsync(StartupItem item, CancellationToken cancellationToken);
    Task<StartupActionResult> StartDefenderQuickScanAsync(StartupItem item, CancellationToken cancellationToken);
}
