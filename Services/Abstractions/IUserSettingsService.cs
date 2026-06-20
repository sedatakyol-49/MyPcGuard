namespace MyPcGuard.Services.Abstractions;

public interface IUserSettingsService
{
    string GetLanguage();
    Task<string> GetLanguageAsync(CancellationToken cancellationToken);
    Task SaveLanguageAsync(string cultureCode, CancellationToken cancellationToken);
}
