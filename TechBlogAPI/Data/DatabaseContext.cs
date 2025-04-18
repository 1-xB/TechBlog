using Microsoft.EntityFrameworkCore;
using TechBlogAPI.Entity;

namespace TechBlogAPI.Data;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public DbSet<Post> Posts { get; set; }
    public DbSet<Author> Authors { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(e => e.PostId);
            entity.Property(e => e.PostId).ValueGeneratedOnAdd().IsRequired();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.AuthorId).IsRequired();
            entity.HasOne(e => e.Author)
                .WithMany(a => a.Posts) // relation one to many
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.Cascade); // When an author is deleted, all their posts are also deleted
        });

        modelBuilder.Entity<Author>(entity =>
        {
            entity.HasKey(e => e.AuthorId);
            entity.Property(e => e.AuthorId).ValueGeneratedOnAdd().IsRequired();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.User)
                .WithOne(u => u.Author).
                HasForeignKey<Author>(e => e.UserId);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).ValueGeneratedOnAdd().IsRequired();
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordSalt).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(30);

            entity.Property(e => e.RefreshToken).IsRequired().HasMaxLength(255);
            entity.Property(e => e.RefreshTokenExpiryDate).IsRequired();
            
            entity.HasOne(e => e.Author)
                .WithOne(a => a.User)
                .HasForeignKey<Author>(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });
    }
}