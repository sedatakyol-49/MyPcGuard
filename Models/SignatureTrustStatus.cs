namespace MyPcGuard.Models;

public enum SignatureTrustStatus
{
    TrustedMicrosoft,
    TrustedVendor,
    Unsigned,
    Unknown,
    NotAccessible
}
