using Microsoft.EntityFrameworkCore;
using OpenBioCardServer.Data;
using OpenBioCardServer.Services;

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
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            });
            
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi(); 
        
        // 注册服务
        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<UserService>();
        builder.Services.AddScoped<AdminService>();
        builder.Services.AddScoped<SystemService>();

        var app = builder.Build();

        // 启动时初始化 Root 用户
        using (var scope = app.Services.CreateScope())
        {
            var systemService = scope.ServiceProvider.GetRequiredService<SystemService>();
            try
            {
                await systemService.EnsureRootUserAsync();
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Failed to initialize root user");
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
