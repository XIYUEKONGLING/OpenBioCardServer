using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenBioCardServer.Configuration;
using OpenBioCardServer.Data;
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

        // 数据库配置
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        if (builder.Environment.IsDevelopment())
        {
            // 开发环境 SQLite
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(connectionString));
        }
        else
        {
            // 生产环境 PgSQL
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));
        }

        builder.Services.AddControllers()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = 
                    Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            });
            
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi(); 
        
        // Configuration
        builder.Services.Configure<AssetSettings>(
            builder.Configuration.GetSection("AssetSettings"));
        
        // Configuration Validator
        builder.Services.AddSingleton<IValidateOptions<AssetSettings>, AssetSettingsValidator>();
        
        // Services
        // builder.Services.AddScoped<...>();
        builder.Services.AddScoped<ClassicAuthService>();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            try
            {
                await context.Database.MigrateAsync();
                // await context.Database.EnsureCreatedAsync();
                
                logger.LogInformation("==> Initialization completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CRITICAL ERROR during database initialization");
                throw;
            }
            
            // Root user initialization
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var rootUsername = config["AuthSettings:RootUsername"];
            var rootPassword = config["AuthSettings:RootPassword"];

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
