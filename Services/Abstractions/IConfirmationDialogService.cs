namespace MyPcGuard.Services.Abstractions;

public interface IConfirmationDialogService
{
    Task<bool> ConfirmAsync(string title, string message, CancellationToken cancellationToken);
}
