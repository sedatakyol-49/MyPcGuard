using MyPcGuard.Models;

namespace MyPcGuard.Services.Abstractions;

public interface IProcessActionService
{
    Task<ActionResult> StopProcessAsync(ProcessScanItem process, CancellationToken cancellationToken);
    Task<ActionResult> OpenFileLocationAsync(ProcessScanItem process, CancellationToken cancellationToken);
}
