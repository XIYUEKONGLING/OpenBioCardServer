using System.Text.Json;
using OpenBioCardServer.Models.DTOs.Classic;
using OpenBioCardServer.Models.Entities;
using OpenBioCardServer.Models.Enums;

namespace OpenBioCardServer.Utilities.Mappers;

public static class ClassicMapper
{
    // === Entity to Classic DTO ===

    public static ClassicProfile ToClassicProfile(ProfileEntity profile)
    {
        return new ClassicProfile
        {
            Username = profile.AccountName,
            UserType = profile.Account != null ? ToUserTypeString(profile.Account.Type) : ToUserTypeString(AccountType.Personal),
            
            Name = profile.Nickname ?? string.Empty,
            Pronouns = profile.Pronouns ?? string.Empty,
            Avatar = AssetToString(profile.AvatarType, profile.AvatarText, profile.AvatarData),
            Bio = profile.Biography ?? string.Empty,
            Location = profile.Location ?? string.Empty,
            Website = profile.Website ?? string.Empty,
            Background = profile.BackgroundType.HasValue 
                ? AssetToString(profile.BackgroundType.Value, profile.BackgroundText, profile.BackgroundData)
                : string.Empty,
            CurrentCompany = profile.CurrentCompany ?? string.Empty,
            CurrentCompanyLink = profile.CurrentCompanyLink ?? string.Empty,
            CurrentSchool = profile.CurrentSchool ?? string.Empty,
            CurrentSchoolLink = profile.CurrentSchoolLink ?? string.Empty,
            Contacts = profile.Contacts.Select(ToClassicContact).ToList(),
            SocialLinks = profile.SocialLinks.Select(ToClassicSocialLink).ToList(),
            Projects = profile.Projects.Select(ToClassicProject).ToList(),
            WorkExperiences = profile.WorkExperiences.Select(ToClassicWorkExperience).ToList(),
            SchoolExperiences = profile.SchoolExperiences.Select(ToClassicSchoolExperience).ToList(),
            Gallery = profile.Gallery.Select(ToClassicGalleryItem).ToList()
        };
    }
    
    public static string AssetToString(AssetType type, string? text, byte[]? data) => type switch
    {
        AssetType.Text => text ?? string.Empty,
        AssetType.Remote => text ?? string.Empty,
        AssetType.Style => text ?? string.Empty,
        AssetType.Image => data != null ? CreateImageDataUrl(data) : string.Empty,
        _ => string.Empty
    };

    private static string CreateImageDataUrl(byte[] data)
    {
        var mimeType = ImageHelper.DetectMimeType(data);
        var base64Length = GetBase64EncodedLength(data.Length);
        var totalLength = 5 + mimeType.Length + 8 + base64Length; // "data:" + mimeType + ";base64," + base64
    
        return string.Create(totalLength, (data, mimeType), static (span, state) =>
        {
            var current = span;
        
            "data:".AsSpan().CopyTo(current);
            current = current[5..];
        
            state.mimeType.AsSpan().CopyTo(current);
            current = current[state.mimeType.Length..];
        
            ";base64,".AsSpan().CopyTo(current);
            current = current[8..];
        
            Convert.TryToBase64Chars(state.data, current, out _);
        });
    }

    private static int GetBase64EncodedLength(int byteLength) => ((byteLength + 2) / 3) * 4;


    private static ClassicContact ToClassicContact(ContactItemEntity c)
    {
        var value = c.ImageType.HasValue
            ? AssetToString(c.ImageType.Value, c.ImageText, c.ImageData)
            : c.Text ?? string.Empty;
        
        return new ClassicContact
        {
            Type = c.Type,
            Value = value
        };
    }

    private static ClassicSocialLink ToClassicSocialLink(SocialLinkItemEntity s)
    {
        Dictionary<string, object>? githubData = null;
        if (s.Type == "github" && !string.IsNullOrEmpty(s.AttributesJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(s.AttributesJson);
                githubData = ConvertJsonElementToObject(doc.RootElement) as Dictionary<string, object>;
            }
            catch { /* Ignore deserialization errors */ }
        }

        return new ClassicSocialLink
        {
            Type = s.Type,
            Value = s.Value,
            GithubData = githubData
        };
    }
    
    // Convert JsonElement to actual .NET types
    private static object? ConvertJsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(
                    prop => prop.Name,
                    prop => ConvertJsonElementToObject(prop.Value)!
                ),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(ConvertJsonElementToObject)
                .ToList(),
            JsonValueKind.String => element.GetString()!,
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => null
        };
    }


    private static ClassicProject ToClassicProject(ProjectItemEntity p) => new()
    {
        Name = p.Name,
        Url = p.Url ?? string.Empty,
        Description = p.Description ?? string.Empty,
        Logo = p.LogoType.HasValue 
            ? AssetToString(p.LogoType.Value, p.LogoText, p.LogoData)
            : string.Empty
    };

    private static ClassicWorkExperience ToClassicWorkExperience(WorkExperienceItemEntity w) => new()
    {
        Position = w.Position ?? string.Empty,
        Company = w.Company,
        CompanyLink = w.CompanyUrl ?? string.Empty,
        StartDate = w.StartDate?.ToString("yyyy-MM-dd") ?? string.Empty,
        EndDate = w.EndDate?.ToString("yyyy-MM-dd") ?? string.Empty,
        Description = w.Description ?? string.Empty,
        Logo = w.LogoType.HasValue
            ? AssetToString(w.LogoType.Value, w.LogoText, w.LogoData)
            : string.Empty
    };

    private static ClassicSchoolExperience ToClassicSchoolExperience(SchoolExperienceItemEntity s) => new()
    {
        Degree = s.Degree ?? string.Empty,
        School = s.School,
        SchoolLink = s.SchoolLink ?? string.Empty,
        Major = s.Major ?? string.Empty,
        StartDate = s.StartDate?.ToString("yyyy-MM-dd") ?? string.Empty,
        EndDate = s.EndDate?.ToString("yyyy-MM-dd") ?? string.Empty,
        Description = s.Description ?? string.Empty,
        Logo = s.LogoType.HasValue
            ? AssetToString(s.LogoType.Value, s.LogoText, s.LogoData)
            : string.Empty
    };

    private static ClassicGalleryItem ToClassicGalleryItem(GalleryItemEntity g) => new()
    {
        Image = g.ImageType.HasValue
            ? AssetToString(g.ImageType.Value, g.ImageText, g.ImageData)
            : string.Empty,
        Caption = g.Caption ?? string.Empty
    };

    // === Classic DTO to Entity ===

    public static void UpdateProfileFromClassic(ProfileEntity profile, ClassicProfile classic)
    {
        profile.AccountName = classic.Username;
        
        if (profile.Account != null)
        {
            profile.Account.Type = ParseUserType(classic.UserType);
        }
        
        profile.Nickname = classic.Name;
        profile.Pronouns = classic.Pronouns;
        
        // Avatar
        var (avatarType, avatarText, avatarData) = ParseAsset(classic.Avatar);
        profile.AvatarType = avatarType;
        profile.AvatarText = avatarText;
        profile.AvatarData = avatarData;

        profile.Biography = classic.Bio;
        profile.Location = classic.Location;
        profile.Website = classic.Website;

        // Background
        if (!string.IsNullOrEmpty(classic.Background))
        {
            var (bgType, bgText, bgData) = ParseAsset(classic.Background);
            profile.BackgroundType = bgType;
            profile.BackgroundText = bgText;
            profile.BackgroundData = bgData;
        }
        else
        {
            profile.BackgroundType = null;
            profile.BackgroundText = null;
            profile.BackgroundData = null;
        }

        profile.CurrentCompany = classic.CurrentCompany;
        profile.CurrentCompanyLink = classic.CurrentCompanyLink;
        profile.CurrentSchool = classic.CurrentSchool;
        profile.CurrentSchoolLink = classic.CurrentSchoolLink;
    }

    public static (AssetType type, string? text, byte[]? data) ParseAsset(string value)
    {
        if (string.IsNullOrEmpty(value))
            return (AssetType.Text, null, null);

        // Check if it's a base64 data URL
        if (value.StartsWith("data:image/"))
        {
            var parts = value.Split(',', 2);
            if (parts.Length == 2)
            {
                try
                {
                    var data = Convert.FromBase64String(parts[1]);
                    return (AssetType.Image, null, data);
                }
                catch
                {
                    // Invalid base64, treat as text
                    return (AssetType.Text, value, null);
                }
            }
        }

        // Check if it's a URL
        if (value.StartsWith("http://") || value.StartsWith("https://"))
        {
            return (AssetType.Remote, value, null);
        }

        // Otherwise treat as text/emoji
        return (AssetType.Text, value, null);
    }

    public static List<ContactItemEntity> ToContactEntities(List<ClassicContact> contacts, Guid profileId) =>
        contacts.Select(c =>
        {
            var (imageType, imageText, imageData) = ParseAsset(c.Value);
            var isImage = imageType != AssetType.Text;

            return new ContactItemEntity
            {
                ProfileId = profileId,
                Type = c.Type,
                Text = isImage ? null : c.Value,
                ImageType = isImage ? imageType : null,
                ImageText = isImage ? imageText : null,
                ImageData = isImage ? imageData : null
            };
        }).ToList();

    public static List<SocialLinkItemEntity> ToSocialLinkEntities(List<ClassicSocialLink> links, Guid profileId) =>
        links.Select(s => new SocialLinkItemEntity
        {
            ProfileId = profileId,
            Type = s.Type,
            Value = s.Value,
            AttributesJson = s.GithubData != null 
                ? JsonSerializer.Serialize(s.GithubData)
                : null
        }).ToList();

    public static List<ProjectItemEntity> ToProjectEntities(List<ClassicProject> projects, Guid profileId) =>
        projects.Select(p =>
        {
            var (logoType, logoText, logoData) = ParseAsset(p.Logo);
            
            return new ProjectItemEntity
            {
                ProfileId = profileId,
                Name = p.Name,
                Url = p.Url,
                Description = p.Description,
                LogoType = string.IsNullOrEmpty(p.Logo) ? null : logoType,
                LogoText = logoText,
                LogoData = logoData
            };
        }).ToList();

    public static List<WorkExperienceItemEntity> ToWorkExperienceEntities(
        List<ClassicWorkExperience> experiences, Guid profileId) =>
        experiences.Select(w =>
        {
            var (logoType, logoText, logoData) = ParseAsset(w.Logo);
            
            return new WorkExperienceItemEntity
            {
                ProfileId = profileId,
                Company = w.Company,
                CompanyUrl = w.CompanyLink,
                Position = w.Position,
                StartDate = ParseDate(w.StartDate),
                EndDate = ParseDate(w.EndDate),
                Description = w.Description,
                LogoType = string.IsNullOrEmpty(w.Logo) ? null : logoType,
                LogoText = logoText,
                LogoData = logoData
            };
        }).ToList();

    public static List<SchoolExperienceItemEntity> ToSchoolExperienceEntities(
        List<ClassicSchoolExperience> experiences, Guid profileId) =>
        experiences.Select(s =>
        {
            var (logoType, logoText, logoData) = ParseAsset(s.Logo);
            
            return new SchoolExperienceItemEntity
            {
                ProfileId = profileId,
                School = s.School,
                SchoolLink = s.SchoolLink,
                Degree = s.Degree,
                Major = s.Major,
                StartDate = ParseDate(s.StartDate),
                EndDate = ParseDate(s.EndDate),
                Description = s.Description,
                LogoType = string.IsNullOrEmpty(s.Logo) ? null : logoType,
                LogoText = logoText,
                LogoData = logoData
            };
        }).ToList();

    public static List<GalleryItemEntity> ToGalleryEntities(List<ClassicGalleryItem> gallery, Guid profileId) =>
        gallery.Select(g =>
        {
            var (imageType, imageText, imageData) = ParseAsset(g.Image);
            
            return new GalleryItemEntity
            {
                ProfileId = profileId,
                ImageType = string.IsNullOrEmpty(g.Image) ? null : imageType,
                ImageText = imageText,
                ImageData = imageData,
                Caption = g.Caption
            };
        }).ToList();

    private static DateOnly? ParseDate(string? dateStr)
    {
        if (string.IsNullOrEmpty(dateStr))
            return null;

        if (DateOnly.TryParse(dateStr, out var date))
            return date;

        return null;
    }
    // === Patch Update Logic ===

    public static void UpdateProfileFromPatch(ProfileEntity profile, ClassicProfilePatch patch)
    {
        // 仅当字段不为 Null 时更新
        // 注意：如果前端传空字符串 ""，这里会更新为空字符串，符合预期（清空字段）
        
        if (patch.Username != null) profile.AccountName = patch.Username;
        
        if (patch.UserType != null && profile.Account != null)
        {
            profile.Account.Type = ParseUserType(patch.UserType);
        }
        
        if (patch.Name != null) profile.Nickname = patch.Name;
        if (patch.Pronouns != null) profile.Pronouns = patch.Pronouns;

        if (patch.Avatar != null)
        {
            var (avatarType, avatarText, avatarData) = ParseAsset(patch.Avatar);
            profile.AvatarType = avatarType;
            profile.AvatarText = avatarText;
            profile.AvatarData = avatarData;
        }

        if (patch.Bio != null) profile.Biography = patch.Bio;
        if (patch.Location != null) profile.Location = patch.Location;
        if (patch.Website != null) profile.Website = patch.Website;

        if (patch.Background != null)
        {
            // 如果传空字符串，视为删除背景
            if (string.IsNullOrEmpty(patch.Background))
            {
                profile.BackgroundType = null;
                profile.BackgroundText = null;
                profile.BackgroundData = null;
            }
            else
            {
                var (bgType, bgText, bgData) = ParseAsset(patch.Background);
                profile.BackgroundType = bgType;
                profile.BackgroundText = bgText;
                profile.BackgroundData = bgData;
            }
        }

        if (patch.CurrentCompany != null) profile.CurrentCompany = patch.CurrentCompany;
        if (patch.CurrentCompanyLink != null) profile.CurrentCompanyLink = patch.CurrentCompanyLink;
        if (patch.CurrentSchool != null) profile.CurrentSchool = patch.CurrentSchool;
        if (patch.CurrentSchoolLink != null) profile.CurrentSchoolLink = patch.CurrentSchoolLink;
    }
    
    // === Helpers for Account Type ===
    private static string ToUserTypeString(AccountType type) => type switch
    {
        AccountType.Company => "company",
        AccountType.Organization => "organization",
        AccountType.Personal => "personal",
        _ => "personal" // Default Unknown or others to personal
    };
    
    private static AccountType ParseUserType(string? type) => type?.ToLowerInvariant() switch
    {
        "company" => AccountType.Company,
        "organization" => AccountType.Organization,
        "personal" => AccountType.Personal,
        _ => AccountType.Personal // Default to Personal as requested
    };
}
