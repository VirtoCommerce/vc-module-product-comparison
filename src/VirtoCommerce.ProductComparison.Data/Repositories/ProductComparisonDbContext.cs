using System.Reflection;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.Platform.Data.Extensions;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.ProductComparison.Data.Models;

namespace VirtoCommerce.ProductComparison.Data.Repositories;

public class ProductComparisonDbContext : DbContextBase
{
    public ProductComparisonDbContext(DbContextOptions<ProductComparisonDbContext> options)
        : base(options)
    {
    }

    protected ProductComparisonDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CompareListEntity>().ToAuditableEntityTable("CompareList");
        // A NULL organization never collides in a unique index, so a list without one isn't protected from a
        // concurrent duplicate; accepted as rare (VCST-5918).
        modelBuilder.Entity<CompareListEntity>().HasIndex(x => new { x.UserId, x.StoreId, x.OrganizationId }).IsUnique();
        // On SQL Server EF filters NULLs out of the unique index above, so the lookup of a list without an organization
        // (every guest's) needs its own index.
        modelBuilder.Entity<CompareListEntity>().HasIndex(x => x.UserId);
        modelBuilder.Entity<CompareListEntity>().HasIndex(x => new { x.IsAnonymous, x.ModifiedDate });

        modelBuilder.Entity<CompareListItemEntity>().ToAuditableEntityTable("CompareListItem");
        modelBuilder.Entity<CompareListItemEntity>()
            .HasOne(x => x.CompareList)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.CompareListId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CompareListItemEntity>().HasIndex(x => new { x.CompareListId, x.ProductId, x.ConfigurationKey }).IsUnique();
        modelBuilder.Entity<CompareListItemEntity>().HasIndex(x => x.ProductId);

        modelBuilder.Entity<CompareListItemSectionEntity>().ToEntityTable("CompareListItemSection");
        modelBuilder.Entity<CompareListItemSectionEntity>()
            .HasOne(x => x.CompareListItem)
            .WithMany(x => x.Sections)
            .HasForeignKey(x => x.CompareListItemId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        switch (Database.ProviderName)
        {
            case "Pomelo.EntityFrameworkCore.MySql":
                modelBuilder.ApplyConfigurationsFromAssembly(Assembly.Load("VirtoCommerce.ProductComparison.Data.MySql"));
                break;
            case "Npgsql.EntityFrameworkCore.PostgreSQL":
                modelBuilder.ApplyConfigurationsFromAssembly(Assembly.Load("VirtoCommerce.ProductComparison.Data.PostgreSql"));
                break;
            case "Microsoft.EntityFrameworkCore.SqlServer":
                modelBuilder.ApplyConfigurationsFromAssembly(Assembly.Load("VirtoCommerce.ProductComparison.Data.SqlServer"));
                break;
        }
    }
}
