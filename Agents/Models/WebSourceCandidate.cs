namespace MyPcGuard.Agents.Models;

public sealed record WebSourceCandidate
{
    public string Url { get; init; } = string.Empty;
    public string Domain { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public SourceType SourceType { get; init; } = SourceType.Unknown;
    public VerificationStatus VerificationStatus { get; init; } = VerificationStatus.Unverified;
    public double ConfidenceScore { get; init; }
    public string Reason { get; init; } = string.Empty;
}
