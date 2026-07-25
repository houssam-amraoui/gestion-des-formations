using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TrainingManagement.Infrastructure.Identity;
using TrainingManagement.Domain.Entities;

namespace TrainingManagement.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Training> Trainings => Set<Training>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(user => user.LastName).HasMaxLength(100).IsRequired();
            entity.Property(user => user.ProfilePictureUrl).HasMaxLength(2048);
        });

        builder.Entity<Category>(entity =>
        {
            entity.Property(category => category.Name).HasMaxLength(150).IsRequired();
            entity.Property(category => category.Slug).HasMaxLength(180).IsRequired();
            entity.Property(category => category.Description).HasMaxLength(1000);
            entity.Property(category => category.ImageUrl).HasMaxLength(500);
            entity.HasIndex(category => category.Name).IsUnique();
            entity.HasIndex(category => category.Slug).IsUnique();
        });

        builder.Entity<Training>(entity =>
        {
            entity.Property(training => training.Title).HasMaxLength(200).IsRequired();
            entity.Property(training => training.Slug).HasMaxLength(220).IsRequired();
            entity.Property(training => training.ShortDescription).HasMaxLength(500).IsRequired();
            entity.Property(training => training.Description).IsRequired();
            entity.Property(training => training.ThumbnailUrl).HasMaxLength(500);
            entity.Property(training => training.Language).HasMaxLength(50).IsRequired();
            entity.Property(training => training.Price).HasPrecision(18, 2);
            entity.HasIndex(training => training.Slug).IsUnique();
            entity.HasOne(training => training.Category)
                .WithMany(category => category.Trainings)
                .HasForeignKey(training => training.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany(user => user.Trainings)
                .HasForeignKey(training => training.TrainerId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
