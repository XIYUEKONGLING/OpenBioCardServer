using Microsoft.EntityFrameworkCore;
using OpenBioCardServer.Data;

namespace OpenBioCardServer;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        var allowedOrigins = builder.Configuration
            .GetSection("CorsSettings:AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                // 判断通配符 "*"
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
            // 开发环境：SQLite
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(connectionString));
        }
        else
        {
            // 生产环境：PostgreSQL
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));
        }

        builder.Services.AddControllers()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            });
            
        // Swagger/OpenAPI
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi(); 

        var app = builder.Build();


        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            // app.UseSwaggerUI(); 
        }

        app.UseHttpsRedirection();
        
        app.UseCors("AllowFrontend");
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}
