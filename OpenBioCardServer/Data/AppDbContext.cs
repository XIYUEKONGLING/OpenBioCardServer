using Microsoft.EntityFrameworkCore;
using OpenBioCardServer.Models.Entities;

namespace OpenBioCardServer.Data;

public class AppDbContext : DbContext
{
    private readonly string _databaseType;
    
    public AppDbContext(DbContextOptions<AppDbContext> options, IConfiguration configuration) 
        : base(options)
    {
        _databaseType = configuration.GetValue<string>("DatabaseSettings:Type") ?? "SQLite";
    }

    // DbSets
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Token> Tokens => Set<Token>();
    public DbSet<ProfileEntity> Profiles => Set<ProfileEntity>();
    public DbSet<ContactItemEntity> ContactItems => Set<ContactItemEntity>();
    public DbSet<SocialLinkItemEntity> SocialLinkItems => Set<SocialLinkItemEntity>();
    public DbSet<ProjectItemEntity> ProjectItems => Set<ProjectItemEntity>();
    public DbSet<WorkExperienceItemEntity> WorkExperienceItems => Set<WorkExperienceItemEntity>();
    public DbSet<SchoolExperienceItemEntity> SchoolExperienceItems => Set<SchoolExperienceItemEntity>();
    public DbSet<GalleryItemEntity> GalleryItems => Set<GalleryItemEntity>();
    
    // DbSets (System Settings)
    public DbSet<SystemSettingsEntity> SystemSettings => Set<SystemSettingsEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Account configuration
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasIndex(e => e.AccountName).IsUnique();
            
            entity.HasMany(e => e.Profiles)
                .WithOne(e => e.Account)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.Tokens)
                .WithOne(e => e.Account)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Token configuration
        modelBuilder.Entity<Token>(entity =>
        {
            // Unique index on TokenValue for fast validation
            entity.HasIndex(e => e.TokenValue).IsUnique();
            
            // Index on LastUsed for cleanup queries
            entity.HasIndex(e => e.LastUsed);
        });

        // Profile configuration
        modelBuilder.Entity<ProfileEntity>(entity =>
        {
            // Unique index on Username (redundant with Account but useful)
            entity.HasIndex(e => new { e.AccountName, e.Language }).IsUnique();
            
            // One-to-Many relationships with all child entities
            entity.HasMany(e => e.Contacts)
                .WithOne(e => e.Profile)
                .HasForeignKey(e => e.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.SocialLinks)
                .WithOne(e => e.Profile)
                .HasForeignKey(e => e.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.Projects)
                .WithOne(e => e.Profile)
                .HasForeignKey(e => e.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.WorkExperiences)
                .WithOne(e => e.Profile)
                .HasForeignKey(e => e.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.SchoolExperiences)
                .WithOne(e => e.Profile)
                .HasForeignKey(e => e.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.Gallery)
                .WithOne(e => e.Profile)
                .HasForeignKey(e => e.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SocialLinkItem JSON column configuration
        modelBuilder.Entity<SocialLinkItemEntity>(entity =>
        {
            var columnType = _databaseType.ToUpperInvariant() switch
            {
                "PGSQL" => "jsonb",
                "MYSQL" => "json",
                _ => "text" // SQLite and default
            };
            
            entity.Property(e => e.AttributesJson)
                .HasColumnType(columnType);
        });
        
        // System Settings configuration
        modelBuilder.Entity<SystemSettingsEntity>(entity =>
        {
            // Ensure only one record can exist with Id = 1
            entity.HasIndex(e => e.Id).IsUnique();
        });
    }
    
    public string GetDatabaseType() => _databaseType;
}
