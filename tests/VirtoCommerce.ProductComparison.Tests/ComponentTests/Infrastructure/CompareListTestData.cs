using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.ProductComparison.Core;
using VirtoCommerce.ProductComparison.Core.Models;
using CatalogModuleConstants = VirtoCommerce.CatalogModule.Core.ModuleConstants;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;

/// <summary>New compare lists, items and sections, shaped as the X-API saves them, for seeding through the service.</summary>
internal static class CompareListTestData
{
    public static CompareList GuestList(string guestId, string storeId = ProductComparisonTestContext.StoreId, IList<CompareListItem> items = null)
    {
        return List(guestId, isAnonymous: true, organizationId: null, storeId, items);
    }

    public static CompareList AccountList(string userId, string organizationId = null, string storeId = ProductComparisonTestContext.StoreId, IList<CompareListItem> items = null)
    {
        return List(userId, isAnonymous: false, organizationId, storeId, items);
    }

    /// <param name="configurationKey">The key the X-API derives from the sections; "" for a product without one.</param>
    public static CompareListItem Item(string productId, string categoryId = null, int position = 0, string configurationKey = "", IList<CompareListItemSection> sections = null)
    {
        return new CompareListItem
        {
            ProductId = productId,
            CategoryId = categoryId,
            ConfigurationKey = configurationKey,
            Position = position,
            Sections = sections?.ToList() ?? [],
        };
    }

    public static CompareListItemSection ProductSection(string sectionId, string optionProductId, int quantity = 1)
    {
        return new CompareListItemSection
        {
            SectionId = sectionId,
            Type = CatalogModuleConstants.ConfigurationSectionTypeProduct,
            ProductId = optionProductId,
            Quantity = quantity,
        };
    }

    public static CompareListItemSection TextSection(string sectionId, string customText)
    {
        return new CompareListItemSection
        {
            SectionId = sectionId,
            Type = CatalogModuleConstants.ConfigurationSectionTypeText,
            CustomText = customText,
        };
    }

    private static CompareList List(string userId, bool isAnonymous, string organizationId, string storeId, IList<CompareListItem> items)
    {
        return new CompareList
        {
            StoreId = storeId,
            UserId = userId,
            OrganizationId = organizationId,
            IsAnonymous = isAnonymous,
            Items = items?.ToList() ?? [],
        };
    }
}
