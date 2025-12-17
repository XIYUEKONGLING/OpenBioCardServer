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

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();

            // 配置 JSON 列
            entity.OwnsMany(u => u.Contacts, b => b.ToJson());
            
            // SocialLinks 需要特殊配置，因为包含嵌套对象
            entity.OwnsMany(u => u.SocialLinks, nav =>
            {
                nav.ToJson();
                // 配置 GithubData 作为嵌套的 owned entity
                nav.OwnsOne(s => s.GithubData);
            });
            
            entity.OwnsMany(u => u.Projects, b => b.ToJson());
            entity.OwnsMany(u => u.Gallery, b => b.ToJson());
        });
    }
}