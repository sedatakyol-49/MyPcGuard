using MyPcGuard.Models;

namespace MyPcGuard.Services.Abstractions;

public interface IActionHistoryService
{
    Task<IReadOnlyList<ActionHistoryItem>> GetHistoryAsync(CancellationToken cancellationToken);
    Task AddAsync(ActionHistoryItem item, CancellationToken cancellationToken);
}
