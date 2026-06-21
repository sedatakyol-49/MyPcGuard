using MyPcGuard.Agents.Abstractions;
using MyPcGuard.Agents.Models;

namespace MyPcGuard.Agents.Core;

public sealed class AgentOrchestrator(IEnumerable<IAgent> agents, IAgentMemoryService memoryService) : IAgentOrchestrator
{
    public async Task<HealthReport> AnalyzeAsync(AgentContext context, CancellationToken cancellationToken)
    {
        if (!context.AgentRecommendationsEnabled)
        {
            return new HealthReport();
        }

        var tasks = agents
            .Where(agent => agent.Category != AgentCategory.WebResearch || context.OnlineResearchAllowed)
            .Select(agent => agent.AnalyzeAsync(context, cancellationToken));

        var results = await Task.WhenAll(tasks);
        var report = new HealthReport
        {
            AgentResults = results
                .Where(result => result.Findings.Count > 0 || result.Recommendations.Count > 0)
                .OrderByDescending(result => RiskScore(result.RiskLevel))
                .ThenBy(result => result.AgentName)
                .ToList()
        };

        await memoryService.SaveResultsAsync(report.AgentResults, cancellationToken);
        return report;
    }

    private static int RiskScore(MyPcGuard.Models.RiskLevel riskLevel)
    {
        return riskLevel switch
        {
            MyPcGuard.Models.RiskLevel.Critical => 5,
            MyPcGuard.Models.RiskLevel.High => 4,
            MyPcGuard.Models.RiskLevel.Medium => 3,
            MyPcGuard.Models.RiskLevel.Low => 2,
            MyPcGuard.Models.RiskLevel.Info => 1,
            _ => 0
        };
    }
}
