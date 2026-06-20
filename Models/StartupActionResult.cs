namespace MyPcGuard.Models;

public sealed record StartupActionResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public StartupItem? UpdatedItem { get; init; }

    public static StartupActionResult Ok(string message, StartupItem? updatedItem = null)
    {
        return new StartupActionResult { Success = true, Message = message, UpdatedItem = updatedItem };
    }

    public static StartupActionResult Fail(string message)
    {
        return new StartupActionResult { Success = false, Message = message };
    }
}
