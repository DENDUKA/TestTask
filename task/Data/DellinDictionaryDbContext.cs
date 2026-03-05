using System.Reflection;
using Microsoft.EntityFrameworkCore;
using task.Entities;

namespace task.Data;

public class DellinDictionaryDbContext(DbContextOptions<DellinDictionaryDbContext> options) : DbContext(options)
{
    public DbSet<Office> Offices { get; set; }
    public DbSet<Phone> Phones { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        
        builder.Entity<Office>()
            .OwnsOne(o => o.Coordinates);

        builder.Entity<Office>()
            .HasMany(o => o.Phones)
            .WithOne(p => p.Office)
            .HasForeignKey(p => p.OfficeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
