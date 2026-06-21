using MyPcGuard.Models;

namespace MyPcGuard.Agents.Models;

public sealed record AgentFinding
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public AgentCategory Category { get; init; }
    public RiskLevel RiskLevel { get; init; } = RiskLevel.Info;
    public string Evidence { get; init; } = string.Empty;
    public double ConfidenceScore { get; init; }
    public bool IsActionable { get; init; }
}
