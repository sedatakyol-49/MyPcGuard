using MyPcGuard.Models;

namespace MyPcGuard.Agents.Models;

public sealed record AgentResult
{
    public string AgentName { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<AgentFinding> Findings { get; init; } = [];
    public IReadOnlyList<AgentRecommendation> Recommendations { get; init; } = [];
    public double ConfidenceScore { get; init; }
    public RiskLevel RiskLevel { get; init; } = RiskLevel.Info;
    public bool RequiresUserApproval { get; init; } = true;
    public IReadOnlyList<ActionPlan> SuggestedActionPlans { get; init; } = [];
    public IReadOnlyList<WebSourceCandidate> Sources { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;
}
