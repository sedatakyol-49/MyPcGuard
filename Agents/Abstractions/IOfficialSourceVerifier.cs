using MyPcGuard.Agents.Models;

namespace MyPcGuard.Agents.Abstractions;

public interface IOfficialSourceVerifier
{
    WebSourceCandidate Verify(WebSourceCandidate candidate, AgentCategory category);
}
