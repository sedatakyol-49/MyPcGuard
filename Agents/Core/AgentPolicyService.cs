using MyPcGuard.Agents.Abstractions;
using MyPcGuard.Agents.Models;

namespace MyPcGuard.Agents.Core;

public sealed class AgentPolicyService : IAgentPolicyService
{
    private static readonly HashSet<AgentPolicyRule> DefaultRules =
    [
        AgentPolicyRule.NoAutomaticSystemChanges,
        AgentPolicyRule.NoAutomaticDriverDownloads,
        AgentPolicyRule.NoThirdPartyDriverSites,
        AgentPolicyRule.NoPermanentDeleteWithoutExplicitDangerConfirmation,
        AgentPolicyRule.NoSystemFileQuarantine,
        AgentPolicyRule.OfficialSourcesOnlyForDrivers,
        AgentPolicyRule.LocalOnlyByDefault,
        AgentPolicyRule.OnlineResearchRequiresConsent
    ];

    private static readonly HashSet<string> RejectedDriverDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "driveridentifier.com",
        "driverpack.io",
        "drp.su",
        "iobit.com",
        "driverbooster.com",
        "driverscape.com",
        "driverguide.com",
        "softpedia.com",
        "filehippo.com",
        "uptodown.com",
        "majorgeeks.com",
        "station-drivers.com"
    };

    public IReadOnlySet<AgentPolicyRule> Rules => DefaultRules;

    public bool IsActionAllowed(AgentActionType actionType, AgentCategory category, WebSourceCandidate? source = null)
    {
        if (category == AgentCategory.DriverCheck)
        {
            if (source is not null && IsRejectedDriverSource(source))
            {
                return false;
            }

            return actionType is AgentActionType.OpenWindowsUpdate
                or AgentActionType.OpenDeviceManager
                or AgentActionType.ShowOfficialManufacturerPage
                or AgentActionType.ExportDriverReport
                or AgentActionType.ExportReport
                or AgentActionType.None;
        }

        return actionType is not AgentActionType.None || category is AgentCategory.Reporting or AgentCategory.WebResearch;
    }

    public string ExplainPolicy(AgentActionType actionType, AgentCategory category)
    {
        if (category == AgentCategory.DriverCheck)
        {
            return "Driver policy: MyPcGuard detects driver issues and can show Windows Update, Device Manager or verified official manufacturer pages only. It never downloads or installs drivers automatically.";
        }

        return "Agent policy: system changes require explicit user approval and action history.";
    }

    private static bool IsRejectedDriverSource(WebSourceCandidate source)
    {
        if (source.SourceType is SourceType.DownloadMirror)
        {
            return true;
        }

        if (source.VerificationStatus is VerificationStatus.Rejected)
        {
            return true;
        }

        return RejectedDriverDomains.Any(domain => source.Domain.EndsWith(domain, StringComparison.OrdinalIgnoreCase));
    }
}
