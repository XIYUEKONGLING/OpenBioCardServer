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
using OpenBioCardServer.Constants;
using OpenBioCardServer.Interfaces;
using OpenBioCardServer.Structs.ENums;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace OpenBioCardServer;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1. 注册配置与校验 (Configuration Registration & Validation)
        RegisterConfigurationSettings(builder);

        // 2. 注册各类服务 (Service Registration)
        RegisterServices(builder);

        var app = builder.Build();

        // 3. 启动时验证配置 (Validate Configurations on Startup)
        ValidateAppSettings(app);

        // 4. 初始化数据库与种子数据 (Database Initialization & Seeding)
        await InitializeApplicationDataAsync(app);

        // 5. 配置 HTTP 请求管道 (Configure Middleware Pipeline)
        ConfigureMiddleware(app);

        await app.RunAsync();
    }

    /// <summary>
    /// 注册配置类及其验证器
    /// </summary>
    private static void RegisterConfigurationSettings(WebApplicationBuilder builder)
    {
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
    }

    /// <summary>
    /// 注册应用程序所需的所有服务
    /// </summary>
    private static void RegisterServices(WebApplicationBuilder builder)
    {
        ConfigureCorsService(builder);
        ConfigureRateLimitingService(builder);
        
        // Database Service
        builder.Services.AddDatabaseContext(builder.Configuration);
        
        ConfigureCacheService(builder);
        ConfigureCompressionService(builder);
        
        // MVC & API
        builder.Services.AddControllers()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi();
        
        builder.Services.AddHealthChecks();

        // App Services
        builder.Services.AddScoped<ClassicAuthService>();
        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<ClassicProfileService>();
        
        builder.Services.AddHostedService<TokenCleanupService>();
    }

    private static void ConfigureCorsService(WebApplicationBuilder builder)
    {
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
    }

    private static void ConfigureRateLimitingService(WebApplicationBuilder builder)
    {
        var settings = builder.Configuration.GetSection(RateLimitSettings.SectionName).Get<RateLimitSettings>() ?? new RateLimitSettings();
        
        builder.Services.AddRateLimiter(options =>
        {
            // 用于记录已注册的策略名称，防止重复注册或用于检测缺失
            var registeredPolicies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 注册配置文件中定义的策略
            if (settings.Policies != null)
            {
                foreach (var policy in settings.Policies)
                {
                    if (registeredPolicies.Contains(policy.PolicyName)) continue;

                    switch (policy.Type)
                    {
                        case RateLimiterType.FixedWindow:
                            options.AddFixedWindowLimiter(policy.PolicyName, opt =>
                            {
                                opt.Window = TimeSpan.FromSeconds(policy.WindowSeconds);
                                opt.PermitLimit = policy.PermitLimit;
                                opt.QueueLimit = policy.QueueLimit;
                                opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                            });
                            break;

                        case RateLimiterType.SlidingWindow:
                            options.AddSlidingWindowLimiter(policy.PolicyName, opt =>
                            {
                                opt.Window = TimeSpan.FromSeconds(policy.WindowSeconds);
                                opt.PermitLimit = policy.PermitLimit;
                                opt.QueueLimit = policy.QueueLimit;
                                opt.SegmentsPerWindow = policy.SegmentsPerWindow;
                                opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                            });
                            break;

                        case RateLimiterType.TokenBucket:
                            options.AddTokenBucketLimiter(policy.PolicyName, opt =>
                            {
                                opt.TokenLimit = policy.PermitLimit;
                                opt.QueueLimit = policy.QueueLimit;
                                opt.TokensPerPeriod = policy.TokensPerPeriod;
                                opt.ReplenishmentPeriod = TimeSpan.FromSeconds(policy.ReplenishmentPeriodSeconds);
                                opt.AutoReplenishment = true;
                            });
                            break;

                        case RateLimiterType.Concurrency:
                            options.AddConcurrencyLimiter(policy.PolicyName, opt =>
                            {
                                opt.PermitLimit = policy.PermitLimit;
                                opt.QueueLimit = policy.QueueLimit;
                                opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                            });
                            break;
                    }
                    registeredPolicies.Add(policy.PolicyName);
                }
            }

            // 默认策略组
            {
                // 默认 Login 策略: 5次 / 分钟 (防暴力破解)
                if (!registeredPolicies.Contains(RateLimitPolicies.Login))
                {
                    options.AddFixedWindowLimiter(RateLimitPolicies.Login, opt =>
                    {
                        opt.Window = TimeSpan.FromMinutes(1);
                        opt.PermitLimit = 5;
                        opt.QueueLimit = 0;
                    });
                }

                // 默认 General 策略: 60次 / 分钟 (普通 API 保护)
                if (!registeredPolicies.Contains(RateLimitPolicies.General))
                {
                    options.AddSlidingWindowLimiter(RateLimitPolicies.General, opt =>
                    {
                        opt.Window = TimeSpan.FromMinutes(1);
                        opt.PermitLimit = 60;
                        opt.SegmentsPerWindow = 6;
                        opt.QueueLimit = 2;
                    });
                }
            }

            // 全局拒绝处理逻辑
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
    }

    private static void ConfigureCacheService(WebApplicationBuilder builder)
    {
        var cacheSettings = builder.Configuration.GetSection(CacheSettings.SectionName).Get<CacheSettings>() ?? new CacheSettings();

        if (!cacheSettings.Enabled)
        {
            Console.WriteLine("==> Cache is DISABLED via configuration.");
            
            builder.Services.AddFusionCache()
                .WithDefaultEntryOptions(new FusionCacheEntryOptions
                {
                    Duration = TimeSpan.Zero,
                    IsFailSafeEnabled = false,
                    FactorySoftTimeout = Timeout.InfiniteTimeSpan,
                    AllowBackgroundDistributedCacheOperations = false
                });
                
            return;
        }
        
        builder.Services.AddMemoryCache(options =>
        {
            options.SizeLimit = cacheSettings.CacheSizeLimit ?? 100;
            options.CompactionPercentage = cacheSettings.CompactionPercentage;
        });

        var fusionBuilder = builder.Services.AddFusionCache()
            .WithOptions(options =>
            {
                options.DistributedCacheCircuitBreakerDuration = TimeSpan.FromSeconds(cacheSettings.DistributedCacheCircuitBreakerDurationSeconds);
            })
            .WithDefaultEntryOptions(new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromMinutes(cacheSettings.ExpirationMinutes),
                
                FactorySoftTimeout = TimeSpan.FromMilliseconds(cacheSettings.FactorySoftTimeoutMilliseconds),
                
                IsFailSafeEnabled = cacheSettings.EnableFailSafe,
                FailSafeMaxDuration = TimeSpan.FromMinutes(cacheSettings.FailSafeMaxDurationMinutes),
                FailSafeThrottleDuration = TimeSpan.FromSeconds(30),
            })
            .WithSerializer(new FusionCacheSystemTextJsonSerializer());
        
        // 根据配置决定是否启用 Redis 和 Backplane
        if (cacheSettings.Enabled && cacheSettings.UseRedis && !string.IsNullOrEmpty(cacheSettings.RedisConnectionString))
        {
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = cacheSettings.RedisConnectionString;
                options.InstanceName = cacheSettings.InstanceName;
            });
            
            // 将 Redis 注册为 FusionCache 的二级缓存
            fusionBuilder.WithRegisteredDistributedCache();
            
            // 注册 Redis Backplane (用于集群缓存同步)
            fusionBuilder.WithStackExchangeRedisBackplane(options =>
            {
                options.Configuration = cacheSettings.RedisConnectionString;
            });
            
            Console.WriteLine("==> FusionCache configured with Redis L2 & Backplane.");
        }
        else
        {
            Console.WriteLine("==> FusionCache configured in Memory-Only mode.");
        }
        // builder.Services.AddSingleton<ICacheService, CacheService>();
    }

    private static void ConfigureCompressionService(WebApplicationBuilder builder)
    {
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
    }

    /// <summary>
    /// 启动时强制验证所有配置，确保配置错误时快速失败
    /// </summary>
    private static void ValidateAppSettings(WebApplication app)
    {
        try
        {
            var dbSettings = app.Services.GetRequiredService<IOptions<DatabaseSettings>>().Value;
            if (!dbSettings.IsValid())
            {
                throw new InvalidOperationException(
                    "Database configuration is invalid. Please check your appsettings.json file.");
            }

            // Trigger validation for other settings
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
            Console.WriteLine("==> CRITICAL ERROR: Configuration validation failed:");
            foreach (var failure in ex.Failures)
            {
                Console.WriteLine($"    - {failure}");
            }
            throw; // Stop application start
        }
    }

    /// <summary>
    /// 初始化数据库结构、Root用户和系统设置
    /// </summary>
    private static async Task InitializeApplicationDataAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var authSettings = scope.ServiceProvider.GetRequiredService<IOptions<AuthSettings>>().Value;

        await InitializeDatabaseAsync(context, logger);
        await InitializeRootUserAsync(context, authSettings, logger);
        await InitializeSystemSettingsAsync(context, logger);
    }

    private static async Task InitializeDatabaseAsync(AppDbContext context, ILogger logger)
    {
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
    }

    private static async Task InitializeRootUserAsync(AppDbContext context, AuthSettings authSettings, ILogger logger)
    {
        if (string.IsNullOrEmpty(authSettings.RootUsername) || string.IsNullOrEmpty(authSettings.RootPassword))
        {
            logger.LogWarning("Root credentials not configured properly");
            return;
        }

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

    private static async Task InitializeSystemSettingsAsync(AppDbContext context, ILogger logger)
    {
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

    /// <summary>
    /// 配置中间件管道
    /// </summary>
    private static void ConfigureMiddleware(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        // 从 DI 容器中获取配置
        var compressionSettings = app.Services.GetRequiredService<IOptions<CompressionSettings>>().Value;
        if (compressionSettings.Enabled)
        {
            app.UseResponseCompression();
        }

        app.UseRateLimiter();
        app.UseCors("AllowFrontend");
        app.UseAuthorization();
        
        app.MapHealthChecks("/health");

        app.MapControllers();
    }
}
