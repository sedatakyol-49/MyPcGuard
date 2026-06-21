using MyPcGuard.Agents.Models;

namespace MyPcGuard.Agents.Abstractions;

public interface IWebResearchAgent : IAgent
{
    Task<IReadOnlyList<WebSourceCandidate>> ResearchOfficialSourcesAsync(string query, CancellationToken cancellationToken);
}
