namespace MyPcGuard.Models;

public sealed record ProcessScanItem
{
    public int ProcessId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Path { get; init; }
    public double CpuUsagePercent { get; init; }
    public long MemoryBytes { get; init; }
    public string Publisher { get; init; } = "Unknown";
    public bool? IsMicrosoftSigned { get; init; }
    public string SignatureStatus { get; init; } = "Unknown";
    public SignatureTrustStatus SignatureTrustStatus { get; init; } = SignatureTrustStatus.Unknown;
}
