using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<Slot> Slots => Set<Slot>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Booking>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Booking>()
            .HasOne(b => b.Slot)
            .WithMany(s => s.Bookings)
            .HasForeignKey(b => b.SlotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Booking>()
            .HasIndex(b => new { b.SlotId, b.Date })
            .IsUnique();

        builder.Entity<Slot>()
            .HasOne(s => s.Resource)
            .WithMany(r => r.Slots)
            .HasForeignKey(s => s.ResourceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
