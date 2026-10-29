using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using VirtoCommerce.ProductComparison.Data.Repositories;

namespace VirtoCommerce.ProductComparison.Data.SqlServer;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ProductComparisonDbContext>
{
    public ProductComparisonDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<ProductComparisonDbContext>();
        var connectionString = args.Length != 0 ? args[0] : "Server=(local);User=virto;Password=virto;Database=VirtoCommerce3;";

        builder.UseSqlServer(
            connectionString,
            options => options.MigrationsAssembly(typeof(SqlServerDataAssemblyMarker).Assembly.GetName().Name));

        return new ProductComparisonDbContext(builder.Options);
    }
}
