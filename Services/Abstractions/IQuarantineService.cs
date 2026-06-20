using MyPcGuard.Models;

namespace MyPcGuard.Services.Abstractions;

public interface IQuarantineService
{
    Task<IReadOnlyList<QuarantineItem>> GetItemsAsync(CancellationToken cancellationToken);
    Task<ActionResult> MoveToQuarantineAsync(string path, string reason, CancellationToken cancellationToken);
    Task<ActionResult> RestoreAsync(QuarantineItem item, CancellationToken cancellationToken);
}
