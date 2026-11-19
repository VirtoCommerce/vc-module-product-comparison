using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using VirtoCommerce.ProductComparison.Core.Models;
using VirtoCommerce.ProductComparison.Xapi.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Schemas;

namespace VirtoCommerce.ProductComparison.Xapi.Schemas;

public class CompareListConfigurationSectionType : ExtendableGraphType<CompareListItemSection>
{
    public CompareListConfigurationSectionType(IDataLoaderContextAccessor dataLoader)
    {
        Field(x => x.SectionId, nullable: false).Description("Configuration section ID");
        Field(x => x.Type, nullable: false).Description("Section type: Product or Text");
        Field(x => x.ProductId, nullable: true).Description("Product ID of the chosen option");
        Field(x => x.Quantity, nullable: true).Description("Quantity of the chosen option");
        Field(x => x.CustomText, nullable: true).Description("Text of a Text section");

        var sectionField = new FieldType
        {
            Name = "configurationSection",
            Description = "The section as the catalog has it now (name, options); null when it no longer exists",
            Type = GraphTypeExtensionHelper.GetActualType<ConfigurationSectionType>(),
            Resolver = new FuncFieldResolver<CompareListItemSection, IDataLoaderResult<ExpProductConfigurationSection>>(context => dataLoader.LoadCompareListConfigurationSection(context)),
        };
        AddField(sectionField);
    }
}
