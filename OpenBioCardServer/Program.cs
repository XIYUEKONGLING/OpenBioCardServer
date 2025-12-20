using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenBioCardServer.Configuration;
using OpenBioCardServer.Data;
using OpenBioCardServer.Extensions;
using OpenBioCardServer.Models;
using OpenBioCardServer.Models.Entities;
using OpenBioCardServer.Models.Enums;
using OpenBioCardServer.Services;
using OpenBioCardServer.Utilities;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using OpenBioCardServer.Interfaces;

namespace OpenBioCardServer;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Configuration Registration & Validation
        
        // Database
        builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection(DatabaseSettings.SectionName));
        builder.Services.AddSingleton<IValidateOptions<DatabaseSettings>, DatabaseSettingsValidator>();
        
        // Cache
        builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection(CacheSettings.SectionName));
        builder.Services.AddSingleton<IValidateOptions<CacheSettings>, CacheSettingsValidator>();
        
        // CORS
        builder.Services.Configure<CorsSettings>(builder.Configuration.GetSection(CorsSettings.SectionName));
        builder.Services.AddSingleton<IValidateOptions<CorsSettings>, CorsSettingsValidator>();
        
        // Compression
        builder.Services.Configure<CompressionSettings>(builder.Configuration.GetSection(CompressionSettings.SectionName));
        builder.Services.AddSingleton<IValidateOptions<CompressionSettings>, CompressionSettingsValidator>();
        
        // Auth
        builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection(AuthSettings.SectionName));
        builder.Services.AddSingleton<IValidateOptions<AuthSettings>, AuthSettingsValidator>();
        
        // Assets
        builder.Services.Configure<AssetSettings>(builder.Configuration.GetSection(AssetSettings.SectionName));
        builder.Services.AddSingleton<IValidateOptions<AssetSettings>, AssetSettingsValidator>();
        
        // Rate Limit
        builder.Services.Configure<RateLimitSettings>(builder.Configuration.GetSection(RateLimitSettings.SectionName));
        builder.Services.AddSingleton<IValidateOptions<RateLimitSettings>, RateLimitSettingsValidator>();
        
        
        // CORS Service
        var corsSettings = builder.Configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>() ?? new CorsSettings();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                if (corsSettings.AllowedOrigins.Contains("*"))
                {
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                }
                else
                {
                    policy.WithOrigins(corsSettings.AllowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials(); 
                }
            });
        });

        // Rate Limiting Service
        var rateLimitSettings = builder.Configuration.GetSection(RateLimitSettings.SectionName).Get<RateLimitSettings>() ?? new RateLimitSettings();
        builder.Services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter(rateLimitSettings.PolicyName, limiterOptions =>
            {
                limiterOptions.Window = TimeSpan.FromMinutes(rateLimitSettings.WindowMinutes);
                limiterOptions.PermitLimit = rateLimitSettings.PermitLimit;
                limiterOptions.QueueLimit = rateLimitSettings.QueueLimit;
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";
                
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
                    ? (int)retryAfterValue.TotalSeconds
                    : 60;

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Too many requests. Please try again later.",
                    retryAfter
                }, cancellationToken);
            };
        });

        // Database Service
        builder.Services.AddDatabaseContext(builder.Configuration);

        // Cache Service
        var cacheSettings = builder.Configuration.GetSection(CacheSettings.SectionName).Get<CacheSettings>() ?? new CacheSettings();
        
        builder.Services.AddMemoryCache(options =>
        {
            options.SizeLimit = cacheSettings.CacheSizeLimit ?? 100;
            options.CompactionPercentage = cacheSettings.CompactionPercentage; // Used from config
        });

        if (cacheSettings.UseRedis && !string.IsNullOrEmpty(cacheSettings.RedisConnectionString))
        {
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = cacheSettings.RedisConnectionString;
                options.InstanceName = cacheSettings.InstanceName;
            });
        }
        builder.Services.AddSingleton<ICacheService, CacheService>();

        // Compression Service
        var compressionSettings = builder.Configuration.GetSection(CompressionSettings.SectionName).Get<CompressionSettings>() ?? new CompressionSettings();
        if (compressionSettings.Enabled)
        {
            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = compressionSettings.EnableForHttps;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });

            var level = compressionSettings.GetCompressionLevel();
            builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = level);
            builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = level);
        }
        
        // MVC & API
        builder.Services.AddControllers()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            });
            
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi(); 
        
        // App Services
        builder.Services.AddScoped<ClassicAuthService>();
        builder.Services.AddScoped<AuthService>();
        builder.Services.AddHostedService<TokenCleanupService>();

        var app = builder.Build();
        
        // Validate ALL configurations on startup (Fail Fast)
        try
        {
            var dbSettings = app.Services.GetRequiredService<IOptions<DatabaseSettings>>().Value;
            if (!dbSettings.IsValid())
            {
                throw new InvalidOperationException(
                    "Database configuration is invalid. Please check your appsettings.json file.");
            }
            
            // Trigger validation for other settings
            // Accessing .Value triggers the Validator registered earlier
            _ = app.Services.GetRequiredService<IOptions<CacheSettings>>().Value;
            _ = app.Services.GetRequiredService<IOptions<AuthSettings>>().Value;
            _ = app.Services.GetRequiredService<IOptions<CorsSettings>>().Value;
            _ = app.Services.GetRequiredService<IOptions<AssetSettings>>().Value;
            _ = app.Services.GetRequiredService<IOptions<RateLimitSettings>>().Value;
            _ = app.Services.GetRequiredService<IOptions<CompressionSettings>>().Value;
            
            Console.WriteLine($"==> Configuration validated.");
            Console.WriteLine($"==> Using database type: {dbSettings.Type}");
            Console.WriteLine($"==> Connection string: {dbSettings.ConnectionString[..Math.Min(50, dbSettings.ConnectionString.Length)]}...");
        }
        catch (OptionsValidationException ex)
        {
            // Console.WriteLine("==> CRITICAL ERROR: Database configuration validation failed:");
            Console.WriteLine("==> CRITICAL ERROR: Configuration validation failed:");
            foreach (var failure in ex.Failures)
            {
                Console.WriteLine($"    - {failure}");
            }
            throw; // Stop application start
        }

        using (var scope = app.Services.CreateScope())
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var authSettings = scope.ServiceProvider.GetRequiredService<IOptions<AuthSettings>>().Value;

            try
            {
                logger.LogInformation("==> Database initialization started...");
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
            if (string.IsNullOrEmpty(authSettings.RootUsername) || string.IsNullOrEmpty(authSettings.RootPassword))
            {
                logger.LogWarning("Root credentials not configured properly");
            }
            else
            {
                using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    var rootAccount = await context.Accounts.FirstOrDefaultAsync(a => a.UserName == authSettings.RootUsername);

                    if (rootAccount == null)
                    {
                        var (hash, salt) = PasswordHasher.HashPassword(authSettings.RootPassword);
                        rootAccount = new Account
                        {
                            UserName = authSettings.RootUsername,
                            PasswordHash = hash,
                            PasswordSalt = salt,
                            Type = UserType.Root
                        };
                        context.Accounts.Add(rootAccount);
                        await context.SaveChangesAsync();
                        logger.LogInformation("==> Root user created: {Username}", authSettings.RootUsername);
                    }
                    else
                    {
                        var (hash, salt) = PasswordHasher.HashPassword(authSettings.RootPassword);
                        rootAccount.PasswordHash = hash;
                        rootAccount.PasswordSalt = salt;
                        await context.SaveChangesAsync();
                        logger.LogInformation("==> Root password updated for: {Username}", authSettings.RootUsername);
                    }
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    logger.LogError(ex, "Error during root user initialization");
                    throw;
                }
            }

            // System settings initialization
            using var settingsTransaction = await context.Database.BeginTransactionAsync();

            try
            {
                var existingSettings = await context.SystemSettings.FindAsync(1);
                if (existingSettings == null)
                {
                    var defaultSettings = new SystemSettingsEntity
                    {
                        Id = 1,
                        Title = SystemSettings.DefaultTitle,
                        LogoType = null,
                        LogoText = null,
                        LogoData = null
                    };

                    context.SystemSettings.Add(defaultSettings);
                    await context.SaveChangesAsync();

                    logger.LogInformation("==> Default system settings initialized");
                }
                
                await settingsTransaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await settingsTransaction.RollbackAsync();
                logger.LogError(ex, "Error during system settings initialization");
                throw;
            }
        }

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        // Enable Response Compression Middleware
        if (compressionSettings.Enabled)
        {
            app.UseResponseCompression();
        }

        app.UseRateLimiter();
        app.UseCors("AllowFrontend");
        app.UseAuthorization();
        app.MapControllers();
        
        await app.RunAsync();
    }
}
