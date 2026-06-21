using MyPcGuard.Agents.Abstractions;
using MyPcGuard.Agents.Models;
using MyPcGuard.Models;

namespace MyPcGuard.Agents.Web;

public sealed class WebResearchAgent(IOfficialSourceVerifier sourceVerifier) : IWebResearchAgent
{
    public string Name => "Web Research Agent";
    public string Description => "Placeholder for optional online research. Disabled by default and never uploads files.";
    public AgentCategory Category => AgentCategory.WebResearch;

    public Task<AgentResult> AnalyzeAsync(AgentContext context, CancellationToken cancellationToken)
    {
        if (!context.OnlineResearchAllowed)
        {
            return Task.FromResult(new AgentResult
            {
                AgentName = Name,
                Summary = "Online research is disabled. Local-only mode is active.",
                ConfidenceScore = 1,
                RiskLevel = RiskLevel.Info,
                RequiresUserApproval = false
            });
        }

        return Task.FromResult(new AgentResult
        {
            AgentName = Name,
            Summary = "Online research is enabled, but real web integration is not implemented yet.",
            Recommendations =
            [
                new AgentRecommendation
                {
                    Title = "Official sources only",
                    Description = "Future web research will return source URL, source type and verification status.",
                    Reason = "Driver and program recommendations must not trust web results blindly.",
                    SafetyLevel = AgentSafetyLevel.Safe,
                    ExpectedBenefit = "Safer manual follow-up.",
                    RelatedActionType = AgentActionType.None
                }
            ],
            ConfidenceScore = 0.8,
            RiskLevel = RiskLevel.Info,
            RequiresUserApproval = false
        });
    }

    public Task<ActionPlan?> BuildActionPlanAsync(AgentContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult<ActionPlan?>(null);
    }

    public Task<IReadOnlyList<WebSourceCandidate>> ResearchOfficialSourcesAsync(string query, CancellationToken cancellationToken)
    {
        var candidate = sourceVerifier.Verify(new WebSourceCandidate
        {
            Url = query,
            Domain = query,
            Title = query,
            SourceType = SourceType.Unknown,
            VerificationStatus = VerificationStatus.Unverified,
            ConfidenceScore = 0.2,
            Reason = "Placeholder candidate; no network request was made."
        }, AgentCategory.DriverCheck);

        return Task.FromResult<IReadOnlyList<WebSourceCandidate>>([candidate]);
    }
}
