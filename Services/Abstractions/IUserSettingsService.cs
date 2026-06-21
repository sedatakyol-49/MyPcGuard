namespace MyPcGuard.Services.Abstractions;

public interface IUserSettingsService
{
    string GetLanguage();
    Task<string> GetLanguageAsync(CancellationToken cancellationToken);
    Task SaveLanguageAsync(string cultureCode, CancellationToken cancellationToken);
    Task<UserSettingsSnapshot> GetSettingsAsync(CancellationToken cancellationToken);
    Task SaveSettingsAsync(UserSettingsSnapshot settings, CancellationToken cancellationToken);
}

public sealed record UserSettingsSnapshot
{
    public string Language { get; init; } = "de";
    public bool AgentRecommendationsEnabled { get; init; } = true;
    public bool OnlineResearchAllowed { get; init; }
    public bool OfficialSourcesOnly { get; init; } = true;
    public bool RememberIgnoredRecommendations { get; init; } = true;
}
