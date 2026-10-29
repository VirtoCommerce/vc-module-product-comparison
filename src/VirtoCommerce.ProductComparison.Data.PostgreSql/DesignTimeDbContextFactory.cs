using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using VirtoCommerce.ProductComparison.Data.Repositories;

namespace VirtoCommerce.ProductComparison.Data.PostgreSql;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ProductComparisonDbContext>
{
    public ProductComparisonDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<ProductComparisonDbContext>();
        var connectionString = args.Length != 0 ? args[0] : "Server=localhost;Username=virto;Password=virto;Database=VirtoCommerce3;";

        builder.UseNpgsql(
            connectionString,
            options => options.MigrationsAssembly(typeof(PostgreSqlDataAssemblyMarker).Assembly.GetName().Name));

        return new ProductComparisonDbContext(builder.Options);
    }
}
