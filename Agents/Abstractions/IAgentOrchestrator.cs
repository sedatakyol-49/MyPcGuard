using MyPcGuard.Agents.Models;

namespace MyPcGuard.Agents.Abstractions;

public interface IAgentOrchestrator
{
    Task<HealthReport> AnalyzeAsync(AgentContext context, CancellationToken cancellationToken);
}
