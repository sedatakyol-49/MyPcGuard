using MyPcGuard.Agents.Models;

namespace MyPcGuard.Agents.Abstractions;

public interface IAgent
{
    string Name { get; }
    string Description { get; }
    AgentCategory Category { get; }
    Task<AgentResult> AnalyzeAsync(AgentContext context, CancellationToken cancellationToken);
    Task<ActionPlan?> BuildActionPlanAsync(AgentContext context, CancellationToken cancellationToken);
}
