using MyPcGuard.Models;

namespace MyPcGuard.Services.Abstractions;

public interface ILocalizationService
{
    string CurrentCulture { get; }
    event EventHandler? CultureChanged;
    string GetString(string key);
    string GetString(string key, params object[] args);
    void SetCulture(string cultureCode);
    IReadOnlyList<SupportedLanguage> GetSupportedLanguages();
}
