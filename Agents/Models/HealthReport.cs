namespace MyPcGuard.Agents.Models;

public sealed record HealthReport
{
    public IReadOnlyList<AgentResult> AgentResults { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;
}
