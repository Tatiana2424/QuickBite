using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using QuickBite.BuildingBlocks.Common;

namespace QuickBite.Identity.Infrastructure;

public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        optionsBuilder.UseSqlServer(DesignTimeSqlServer.ResolveConnectionString("QuickBiteIdentityDb"));
        return new IdentityDbContext(optionsBuilder.Options);
    }
}
