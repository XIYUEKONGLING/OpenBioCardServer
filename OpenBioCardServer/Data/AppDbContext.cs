using Microsoft.EntityFrameworkCore;
using OpenBioCardServer.Models.Entities;

namespace OpenBioCardServer.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Account configuration
        modelBuilder.Entity<Account>(entity =>
        {
            // Unique index on UserName for fast lookup
            entity.HasIndex(e => e.UserName).IsUnique();
            
            // One-to-One with Profile
            entity.HasOne(e => e.Profile)
                .WithOne(e => e.Account)
                .HasForeignKey<ProfileEntity>(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // One-to-Many with Tokens
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
            // Unique index on UserName (redundant with Account but useful)
            entity.HasIndex(e => e.Username).IsUnique();
            
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
        // PostgreSQL uses jsonb, SQLite uses text
        modelBuilder.Entity<SocialLinkItemEntity>(entity =>
        {
            entity.Property(e => e.AttributesJson)
                .HasColumnType(Database.IsNpgsql() ? "jsonb" : "text");
        });
    }
}
