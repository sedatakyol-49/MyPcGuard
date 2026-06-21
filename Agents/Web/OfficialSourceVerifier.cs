using MyPcGuard.Agents.Abstractions;
using MyPcGuard.Agents.Models;

namespace MyPcGuard.Agents.Web;

public sealed class OfficialSourceVerifier : IOfficialSourceVerifier
{
    private static readonly HashSet<string> OfficialDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "microsoft.com",
        "windowsupdate.microsoft.com",
        "intel.com",
        "amd.com",
        "nvidia.com",
        "realtek.com",
        "dell.com",
        "hp.com",
        "lenovo.com",
        "asus.com",
        "logitech.com"
    };

    private static readonly string[] RejectedMarkers =
    [
        "driver-booster",
        "driverbooster",
        "driverpack",
        "driver-updater",
        "driverscape",
        "driverguide",
        "download",
        "mirror",
        "crack",
        "keygen"
    ];

    public WebSourceCandidate Verify(WebSourceCandidate candidate, AgentCategory category)
    {
        var domain = NormalizeDomain(candidate.Domain);

        if (category == AgentCategory.DriverCheck && RejectedMarkers.Any(marker => domain.Contains(marker, StringComparison.OrdinalIgnoreCase) || candidate.Url.Contains(marker, StringComparison.OrdinalIgnoreCase)))
        {
            return candidate with
            {
                Domain = domain,
                SourceType = SourceType.DownloadMirror,
                VerificationStatus = VerificationStatus.Rejected,
                ConfidenceScore = 0,
                Reason = "Rejected by driver policy: third-party driver updater, mirror or unknown executable source."
            };
        }

        var officialMatch = OfficialDomains.FirstOrDefault(official => domain.Equals(official, StringComparison.OrdinalIgnoreCase) || domain.EndsWith("." + official, StringComparison.OrdinalIgnoreCase));
        if (officialMatch is not null)
        {
            return candidate with
            {
                Domain = domain,
                SourceType = officialMatch.Contains("microsoft", StringComparison.OrdinalIgnoreCase) ? SourceType.Microsoft : SourceType.OfficialVendor,
                VerificationStatus = VerificationStatus.Verified,
                ConfidenceScore = 0.95,
                Reason = "Domain matches a known official manufacturer or Microsoft domain."
            };
        }

        return candidate with
        {
            Domain = domain,
            VerificationStatus = VerificationStatus.Unverified,
            ConfidenceScore = Math.Min(candidate.ConfidenceScore, 0.4),
            Reason = "Source is not in the official-domain allow list and must not be recommended for driver downloads."
        };
    }

    private static string NormalizeDomain(string domain)
    {
        if (!string.IsNullOrWhiteSpace(domain))
        {
            return domain.Replace("www.", string.Empty, StringComparison.OrdinalIgnoreCase).Trim().TrimEnd('/');
        }

        return string.Empty;
    }
}
