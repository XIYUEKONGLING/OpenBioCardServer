namespace OpenBioCardServer.Constants;

public class CacheKeys
{
    public const string ClassicPublicSettingsCacheKey = "Classic:System:Settings:Public";
    
    public static string GetProfileCacheKey(string username)
    {
        return $"Profile:{username.Trim().ToLowerInvariant()}";
    }

    public static string GetClassicProfileCacheKey(string accountName, string? language = null)
    {
        // 如果 Language 为 NULL 或空，使用 "@Default" 作为后缀
        var langKey = string.IsNullOrWhiteSpace(language) ? "@Default" : language.Trim().ToLowerInvariant();
        return $"Classic:Profile:{accountName.Trim().ToLowerInvariant()}:{langKey}";
    }
    
}