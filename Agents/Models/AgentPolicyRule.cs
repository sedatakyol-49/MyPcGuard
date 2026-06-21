namespace MyPcGuard.Agents.Models;

public enum AgentPolicyRule
{
    NoAutomaticSystemChanges,
    NoAutomaticDriverDownloads,
    NoThirdPartyDriverSites,
    NoPermanentDeleteWithoutExplicitDangerConfirmation,
    NoSystemFileQuarantine,
    OfficialSourcesOnlyForDrivers,
    LocalOnlyByDefault,
    OnlineResearchRequiresConsent
}
