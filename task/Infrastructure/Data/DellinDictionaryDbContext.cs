using Microsoft.EntityFrameworkCore;
using TestTask.Domain.Entities;

namespace TestTask.Infrastructure.Data;

public class DellinDictionaryDbContext(DbContextOptions<DellinDictionaryDbContext> options) : DbContext(options)
{
    public DbSet<Office> Offices { get; set; }
    public DbSet<Phone> Phones { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<Office>()
            .OwnsOne(o => o.Coordinates);

        builder.Entity<Office>()
            .HasMany(o => o.Phones)
            .WithOne(p => p.Office)
            .HasForeignKey(p => p.OfficeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
