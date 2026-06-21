namespace MyPcGuard.Agents.Models;

public sealed record AgentMemory
{
    public IReadOnlyList<AgentResult> LastResults { get; init; } = [];
    public IReadOnlySet<string> IgnoredRecommendationIds { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.Now;
}
