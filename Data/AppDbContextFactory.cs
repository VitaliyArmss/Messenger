using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Messenger.Data;

public class AppDbContextFactory
    : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        DotNetEnv.Env.TraversePath().Load();

        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                 ?? throw new InvalidOperationException(
                     "ConnectionStrings__DefaultConnection не задан (env/.env)");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(cs);
        return new AppDbContext(optionsBuilder.Options);
    }
}