using Microsoft.EntityFrameworkCore;
using OpenBioCardServer.Models;

namespace OpenBioCardServer.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置 User 实体
        modelBuilder.Entity<User>(entity =>
        {
            // 用户名唯一索引
            entity.HasIndex(u => u.Username).IsUnique();

            // 配置 JSON 列
            entity.OwnsMany(u => u.Contacts, b => b.ToJson());
            entity.OwnsMany(u => u.SocialLinks, b => b.ToJson());
            entity.OwnsMany(u => u.Projects, b => b.ToJson());
            entity.OwnsMany(u => u.Gallery, b => b.ToJson());
        });
    }
}