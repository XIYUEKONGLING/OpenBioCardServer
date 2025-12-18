using Microsoft.EntityFrameworkCore;
using OpenBioCardServer.Configuration;
using OpenBioCardServer.Data;

namespace OpenBioCardServer.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabaseContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var databaseSettings = configuration.GetSection(DatabaseSettings.SectionName)
            .Get<DatabaseSettings>();
        
        if (databaseSettings == null || !databaseSettings.IsValid())
            throw new InvalidOperationException(
                "Database configuration is invalid or missing. " +
                "Please check your appsettings.json file.");
        
        var connectionString = databaseSettings.ConnectionString;
        var databaseType = databaseSettings.Type.ToUpperInvariant();
        
        services.AddDbContext<AppDbContext>(options =>
        {
            switch (databaseType)
            {
                case "SQLITE":
                    options.UseSqlite(connectionString);
                    break;
                    
                case "PGSQL":
                    options.UseNpgsql(connectionString);
                    break;
                    
                case "MYSQL":
                    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                    break;
                    
                default:
                    throw new InvalidOperationException(
                        $"Unsupported database type: {databaseType}. " +
                        "Supported types: SQLite, PgSQL, MySQL");
            }
        });
        
        return services;
    }
}