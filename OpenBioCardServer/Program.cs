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

        // Rate Limiting
        builder.Services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("login", limiterOptions =>
            {
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.PermitLimit = 10;
                limiterOptions.QueueLimit = 0;
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

        // Database configuration with validation
        builder.Services.AddDatabaseContext(builder.Configuration);

        // Cache Configuration (Memory & Redis)
        var cacheSection = builder.Configuration.GetSection("CacheSettings");
        var useRedis = cacheSection.GetValue<bool>("UseRedis");
        var cacheSizeLimit = cacheSection.GetValue<long?>("CacheSizeLimit") ?? 100;
        var redisInstanceName = cacheSection.GetValue<string>("InstanceName") ?? "OpenBioCard:";

        // Configure Local Memory Cache (Always available, with OOM protection)
        builder.Services.AddMemoryCache(options =>
        {
            options.SizeLimit = cacheSizeLimit;
            options.CompactionPercentage = 0.2; // Free up 20% when limit is reached
        });

        // Configure Distributed Cache (Redis) if enabled
        if (useRedis)
        {
            var redisConn = cacheSection.GetValue<string>("RedisConnectionString");
            if (!string.IsNullOrEmpty(redisConn))
            {
                builder.Services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConn;
                    options.InstanceName = redisInstanceName;
                });
            }
        }

        // Response Compression Configuration
        var compressionSection = builder.Configuration.GetSection("CompressionSettings");
        var enableCompression = compressionSection.GetValue<bool>("Enabled", true); // Default to true

        if (enableCompression)
        {
            var enableForHttps = compressionSection.GetValue<bool>("EnableForHttps", true);
            var compressionLevelStr = compressionSection.GetValue<string>("Level") ?? "Fastest";

            // Parse Compression Level Enum
            if (!Enum.TryParse(compressionLevelStr, true, out CompressionLevel compressionLevel))
            {
                compressionLevel = CompressionLevel.Fastest;
            }

            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = enableForHttps;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
                // Optional: Add MIME types if needed, defaults are usually fine for JSON/Text
            });

            // Configure Providers
            builder.Services.Configure<BrotliCompressionProviderOptions>(options => 
                options.Level = compressionLevel);
            
            builder.Services.Configure<GzipCompressionProviderOptions>(options => 
                options.Level = compressionLevel);
        }

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
        builder.Services.AddHostedService<TokenCleanupService>();

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
                using var transaction = await context.Database.BeginTransactionAsync();
                
                try
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
        if (enableCompression)
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
