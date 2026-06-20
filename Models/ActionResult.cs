namespace MyPcGuard.Models;

public sealed record ActionResult
{
    public ActionResultStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool Success => Status == ActionResultStatus.Success;
}
