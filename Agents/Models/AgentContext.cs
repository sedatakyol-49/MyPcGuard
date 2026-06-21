using MyPcGuard.Models;

namespace MyPcGuard.Agents.Models;

public sealed record AgentContext
{
    public ScanResult? ScanResult { get; init; }
    public IReadOnlyList<InstalledProgramItem> InstalledPrograms { get; init; } = [];
    public bool AgentRecommendationsEnabled { get; init; } = true;
    public bool OnlineResearchAllowed { get; init; }
    public bool OfficialSourcesOnly { get; init; } = true;
    public bool RememberIgnoredRecommendations { get; init; } = true;
}
