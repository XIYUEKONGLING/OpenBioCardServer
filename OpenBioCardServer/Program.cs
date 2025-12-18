using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenBioCardServer.Configuration;
using OpenBioCardServer.Data;
using OpenBioCardServer.Extensions;
using OpenBioCardServer.Models.Entities;
using OpenBioCardServer.Models.Enums;
using OpenBioCardServer.Services;
using OpenBioCardServer.Utilities;

namespace OpenBioCardServer;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Configuration validation
        builder.Services.Configure<DatabaseSettings>(
            builder.Configuration.GetSection(DatabaseSettings.SectionName));
        builder.Services.AddSingleton<IValidateOptions<DatabaseSettings>, DatabaseSettingsValidator>();
        
        var allowedOrigins = builder.Configuration
            .GetSection("CorsSettings:AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();
        
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                if (allowedOrigins.Contains("*"))
                {
                    policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
                else
                {
                    policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials(); 
                }
            });
        });

        // Database configuration with validation
        builder.Services.AddDatabaseContext(builder.Configuration);
        
        builder.Services.AddControllers()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            });
            
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi(); 
        
        // Configuration
        builder.Services.Configure<AssetSettings>(
            builder.Configuration.GetSection("AssetSettings"));
        
        // Configuration Validator
        builder.Services.AddSingleton<IValidateOptions<AssetSettings>, AssetSettingsValidator>();
        
        // Services
        builder.Services.AddScoped<ClassicAuthService>();
        builder.Services.AddScoped<AuthService>();

        var app = builder.Build();
        
        // Validate database configuration on startup
        try
        {
            var databaseSettings = app.Services.GetRequiredService<IOptions<DatabaseSettings>>().Value;
            if (!databaseSettings.IsValid())
            {
                throw new InvalidOperationException(
                    "Database configuration is invalid. Please check your appsettings.json file.");
            }
            
            Console.WriteLine($"==> Using database type: {databaseSettings.Type}");
            Console.WriteLine($"==> Connection string: {databaseSettings.ConnectionString[..Math.Min(50, databaseSettings.ConnectionString.Length)]}...");
        }
        catch (OptionsValidationException ex)
        {
            Console.WriteLine("==> CRITICAL ERROR: Database configuration validation failed:");
            foreach (var failure in ex.Failures)
            {
                Console.WriteLine($"    - {failure}");
            }
            throw;
        }

        using (var scope = app.Services.CreateScope())
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            try
            {
                logger.LogInformation("==> Database initialization started...");
                
                // Log database type being used
                var databaseType = context.GetDatabaseType();
                logger.LogInformation("==> Database type: {DatabaseType}", databaseType);
                
                await context.Database.EnsureCreatedAsync();
                await context.Database.MigrateAsync();
                
                logger.LogInformation("==> Database initialization completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CRITICAL ERROR during database initialization");
                throw;
            }
            
            // Root user initialization
            var rootUsername = configuration["AuthSettings:RootUsername"];
            var rootPassword = configuration["AuthSettings:RootPassword"];

            if (string.IsNullOrEmpty(rootUsername) || string.IsNullOrEmpty(rootPassword))
            {
                logger.LogWarning("Root credentials not configured in appsettings");
            }
            else
            {
                var rootAccount = await context.Accounts
                    .FirstOrDefaultAsync(a => a.UserName == rootUsername);

                if (rootAccount == null)
                {
                    // Create root account
                    var (hash, salt) = PasswordHasher.HashPassword(rootPassword);
                    rootAccount = new Account
                    {
                        UserName = rootUsername,
                        PasswordHash = hash,
                        PasswordSalt = salt,
                        Type = UserType.Root
                    };

                    context.Accounts.Add(rootAccount);
                    await context.SaveChangesAsync();
        
                    logger.LogInformation("==> Root user created: {Username}", rootUsername);
                }
                else
                {
                    // Update root password on every startup
                    var (hash, salt) = PasswordHasher.HashPassword(rootPassword);
                    rootAccount.PasswordHash = hash;
                    rootAccount.PasswordSalt = salt;
                    await context.SaveChangesAsync();
        
                    logger.LogInformation("==> Root password updated for: {Username}", rootUsername);
                }
            }
        }

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseCors("AllowFrontend");
        app.UseAuthorization();
        app.MapControllers();
        
        await app.RunAsync();
    }
}
