using CampusEvents.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Event> Events => Set<Event>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("AspNetUsers");

            entity.HasKey(user => user.Id);

            entity.Property(user => user.Id)
                .HasMaxLength(450)
                .ValueGeneratedNever();

            entity.Property(user => user.DisplayName)
                .IsRequired();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(category => category.Name)
                .IsRequired();
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.Property(eventItem => eventItem.Title)
                .IsRequired();

            entity.Property(eventItem => eventItem.Description)
                .IsRequired();

            entity.Property(eventItem => eventItem.Location)
                .IsRequired();

            entity.HasOne(eventItem => eventItem.Category)
                .WithMany(category => category.Events)
                .HasForeignKey(eventItem => eventItem.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(eventItem => eventItem.Organizer)
                .WithMany(user => user.OrganizedEvents)
                .HasForeignKey(eventItem => eventItem.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable("Events", table =>
            {
                table.HasCheckConstraint(
                    "CK_Events_StartBeforeEnd",
                    "[StartDateTime] < [EndDateTime]");

                table.HasCheckConstraint(
                    "CK_Events_CapacityPositive",
                    "[Capacity] > 0");
            });
        });
    }
}
