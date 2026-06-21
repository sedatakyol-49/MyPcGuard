using MyPcGuard.Agents.Abstractions;
using MyPcGuard.Agents.Models;
using MyPcGuard.Models;

namespace MyPcGuard.Agents.Core;

public sealed class PlaceholderAgent(string name, string description, AgentCategory category) : IAgent
{
    public string Name { get; } = name;
    public string Description { get; } = description;
    public AgentCategory Category { get; } = category;

    public Task<AgentResult> AnalyzeAsync(AgentContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new AgentResult
        {
            AgentName = Name,
            Summary = $"{Name} is registered, but detailed analysis is not implemented yet.",
            ConfidenceScore = 0.2,
            RiskLevel = RiskLevel.Info,
            RequiresUserApproval = false
        });
    }

    public Task<ActionPlan?> BuildActionPlanAsync(AgentContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionPlan?>(null);
    }
}
