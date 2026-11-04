using FileShareApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FileShareApp.Data;

public class AppDbContext : IdentityDbContext<IdentityUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<StoredFile> Files => Set<StoredFile>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<StoredFile>(e =>
        {
            e.HasKey(f => f.Id);
            e.Property(f => f.OriginalName).HasMaxLength(260).IsRequired();
            e.Property(f => f.BlobName).HasMaxLength(100).IsRequired();
            e.Property(f => f.ContentType).HasMaxLength(200);
            e.Property(f => f.UploadedByEmail).HasMaxLength(256);
        });
    }
}