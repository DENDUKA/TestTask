using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TestTask.Infrastructure.Data;

public class DellinDictionaryDbContextFactory : IDesignTimeDbContextFactory<DellinDictionaryDbContext>
{
    public DellinDictionaryDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        var builder = new DbContextOptionsBuilder<DellinDictionaryDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        builder.UseNpgsql(connectionString);

        return new DellinDictionaryDbContext(builder.Options);
    }
}
