using System;
using FluentValidation;
using GraphQL.MicrosoftDI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.CatalogModule.Core.Events;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.Jobs;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.MySql.Extensions;
using VirtoCommerce.Platform.Data.PostgreSql.Extensions;
using VirtoCommerce.Platform.Data.SqlServer.Extensions;
using VirtoCommerce.ProductComparison.Core;
using VirtoCommerce.ProductComparison.Core.Services;
using VirtoCommerce.ProductComparison.Data.BackgroundJobs;
using VirtoCommerce.ProductComparison.Data.Handlers;
using VirtoCommerce.ProductComparison.Data.MySql;
using VirtoCommerce.ProductComparison.Data.PostgreSql;
using VirtoCommerce.ProductComparison.Data.Repositories;
using VirtoCommerce.ProductComparison.Data.Services;
using VirtoCommerce.ProductComparison.Data.SqlServer;
using VirtoCommerce.ProductComparison.Xapi;
using VirtoCommerce.ProductComparison.Xapi.Aggregates;
using VirtoCommerce.ProductComparison.Xapi.Authorization;
using VirtoCommerce.ProductComparison.Xapi.Commands;
using VirtoCommerce.ProductComparison.Xapi.Queries;
using VirtoCommerce.ProductComparison.Xapi.Validators;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProductComparison.Web;

public class Module : IModule, IHasConfiguration
{
    public ManifestModuleInfo ModuleInfo { get; set; }
    public IConfiguration Configuration { get; set; }

    public void Initialize(IServiceCollection serviceCollection)
    {
        serviceCollection.AddDbContext<ProductComparisonDbContext>(options =>
        {
            var databaseProvider = Configuration.GetValue("DatabaseProvider", "SqlServer");
            var connectionString = Configuration.GetConnectionString(ModuleInfo.Id) ?? Configuration.GetConnectionString("VirtoCommerce");

            switch (databaseProvider)
            {
                case "MySql":
                    options.UseMySqlDatabase(connectionString, typeof(MySqlDataAssemblyMarker), Configuration);
                    break;
                case "PostgreSql":
                    options.UsePostgreSqlDatabase(connectionString, typeof(PostgreSqlDataAssemblyMarker), Configuration);
                    break;
                default:
                    options.UseSqlServerDatabase(connectionString, typeof(SqlServerDataAssemblyMarker), Configuration);
                    break;
            }
        });

        serviceCollection.AddTransient<IProductComparisonRepository, ProductComparisonRepository>();
        serviceCollection.AddSingleton<Func<IProductComparisonRepository>>(provider => () => provider.CreateScope().ServiceProvider.GetRequiredService<IProductComparisonRepository>());

        serviceCollection.AddTransient<ICompareListService, CompareListService>();
        serviceCollection.AddTransient<ICompareListSearchService, CompareListSearchService>();
        serviceCollection.AddTransient<ProductChangedEventHandler>();

        serviceCollection.AddTransient<CleanupCompareListsJob>();
        serviceCollection.AddRecurringJob<CleanupCompareListsJobHandler, CleanupCompareListsJobPayload>(schedule => schedule
            .WithId(nameof(CleanupCompareListsJob))
            .FromSettings(ModuleConstants.Settings.Cleanup.Enabled, ModuleConstants.Settings.Cleanup.Cron));

        serviceCollection.AddTransient<ICompareListAggregateRepository, CompareListAggregateRepository>();
        serviceCollection.AddSingleton<IAuthorizationHandler, CanAccessCompareListAuthorizationHandler>();
        serviceCollection.AddTransient<AbstractValidator<GetCompareListQuery>, CompareListRequestValidator<GetCompareListQuery>>();
        serviceCollection.AddTransient<AbstractValidator<SaveProductsToCompareListCommand>, SaveProductsToCompareListCommandValidator>();
        serviceCollection.AddTransient<AbstractValidator<RemoveCompareListItemsCommand>, RemoveCompareListItemsCommandValidator>();
        serviceCollection.AddTransient<AbstractValidator<ClearCompareListCommand>, CompareListRequestValidator<ClearCompareListCommand>>();
        serviceCollection.AddTransient<AbstractValidator<ClearCompareListCategoryCommand>, ClearCompareListCategoryCommandValidator>();
        serviceCollection.AddTransient<AbstractValidator<MergeCompareListCommand>, MergeCompareListCommandValidator>();
        serviceCollection.AddTransient<AbstractValidator<MoveCompareListItemCommand>, MoveCompareListItemCommandValidator>();

        _ = new GraphQLBuilder(serviceCollection, builder =>
        {
            builder.AddSchema(serviceCollection, typeof(XapiAssemblyMarker));
        });

        serviceCollection.AddSingleton<ScopedSchemaFactory<XapiAssemblyMarker>>();
    }

    public void PostInitialize(IApplicationBuilder appBuilder)
    {
        var serviceProvider = appBuilder.ApplicationServices;

        var settingsRegistrar = serviceProvider.GetRequiredService<ISettingsRegistrar>();
        settingsRegistrar.RegisterSettings(ModuleConstants.Settings.AllSettings, ModuleInfo.Id);
        settingsRegistrar.RegisterSettingsForType(ModuleConstants.Settings.StoreLevelSettings, nameof(Store));

        appBuilder.UseScopedSchema<XapiAssemblyMarker>("product-comparison");

        appBuilder.RegisterEventHandler<ProductChangedEvent, ProductChangedEventHandler>();

        using var serviceScope = serviceProvider.CreateScope();
        using var dbContext = serviceScope.ServiceProvider.GetRequiredService<ProductComparisonDbContext>();
        dbContext.Database.Migrate();
    }

    public void Uninstall()
    {
        // Nothing to do here
    }
}
