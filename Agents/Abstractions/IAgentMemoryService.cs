using MyPcGuard.Agents.Models;

namespace MyPcGuard.Agents.Abstractions;

public interface IAgentMemoryService
{
    Task<AgentMemory> LoadAsync(CancellationToken cancellationToken);
    Task SaveResultsAsync(IReadOnlyList<AgentResult> results, CancellationToken cancellationToken);
    Task ClearAsync(CancellationToken cancellationToken);
}
