namespace OpenBioCardServer.Configuration;

public class DatabaseSettings
{
    public const string SectionName = "DatabaseSettings";
    
    public string Type { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(Type))
            return false;
            
        if (string.IsNullOrWhiteSpace(ConnectionString))
            return false;
            
        var validTypes = new[] { "SQLite", "PgSQL", "MySQL" };
        return validTypes.Contains(Type, StringComparer.OrdinalIgnoreCase);
    }
}