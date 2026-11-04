using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProductComparison.Core;
using VirtoCommerce.ProductComparison.Core.Models;
using VirtoCommerce.ProductComparison.Xapi.Models;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.XCart.Core.Models;

namespace VirtoCommerce.ProductComparison.Xapi.Aggregates;

public class CompareListAggregate
{
    public CompareList List { get; set; }

    public Store Store { get; set; }

    public string CultureName { get; set; }

    public string CurrencyCode { get; set; }

    public bool IsEnabled { get; set; }

    public bool IsAnonymousListEnabled { get; set; }

    public int MaxItems { get; set; }

    public bool IsSaved => !List.IsTransient();

    // Categories in the order they were first added; each shows at most MaxItems items, as the storefront always did.
    public virtual IList<CompareListCategory> Categories => List.Items
        .GroupBy(x => GetCategoryKey(x.CategoryId), StringComparer.OrdinalIgnoreCase)
        .OrderBy(group => group.Min(x => x.CreatedDate))
        .Select(group =>
        {
            var category = AbstractTypeFactory<CompareListCategory>.TryCreateInstance();
            category.CategoryId = group.First().CategoryId;
            category.Items = group.OrderBy(x => x.Position).ThenBy(x => x.CreatedDate).Take(MaxItems).ToList();
            return category;
        })
        .ToList();

    public virtual IList<CompareListItem> VisibleItems => Categories.SelectMany(x => x.Items).ToList();

    public virtual void EnsureAvailable()
    {
        if (!IsEnabled)
        {
            throw CompareListErrors.Disabled();
        }

        if (List.IsAnonymous && !IsAnonymousListEnabled)
        {
            throw CompareListErrors.AnonymousDisabled();
        }
    }

    public virtual void SaveProducts(IList<CompareListProduct> products)
    {
        var categoryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var product in products)
        {
            var configurationKey = GetConfigurationKey(product.ConfigurationSections);

            if (FindItem(product.ProductId, configurationKey) != null)
            {
                continue;
            }

            var item = AbstractTypeFactory<CompareListItem>.TryCreateInstance();
            item.ProductId = product.ProductId;
            item.CategoryId = string.IsNullOrEmpty(product.CategoryId) ? null : product.CategoryId;
            item.ConfigurationKey = configurationKey;
            item.Position = GetNextPosition(item.CategoryId);
            item.Sections = product.ConfigurationSections?.Select(ToSection).ToList() ?? [];

            List.Items.Add(item);
            categoryKeys.Add(GetCategoryKey(item.CategoryId));
        }

        if (categoryKeys.Any(categoryKey => GetCategoryItems(categoryKey).Count > MaxItems))
        {
            throw CompareListErrors.LimitReached(MaxItems);
        }

        if (List.Items.Count > ModuleConstants.MaxListItems)
        {
            throw CompareListErrors.Full(ModuleConstants.MaxListItems);
        }
    }

    public virtual void RemoveItems(IList<string> itemIds)
    {
        List.Items = List.Items.Where(x => !itemIds.Contains(x.Id, StringComparer.OrdinalIgnoreCase)).ToList();
    }

    public virtual void Clear()
    {
        List.Items = [];
    }

    public virtual void ClearCategory(string categoryId)
    {
        var categoryKey = GetCategoryKey(categoryId);

        List.Items = List.Items.Where(x => !GetCategoryKey(x.CategoryId).EqualsIgnoreCase(categoryKey)).ToList();
    }

    public virtual void MoveItem(string itemId, int position)
    {
        var item = List.Items.FirstOrDefault(x => x.Id.EqualsIgnoreCase(itemId));

        if (item == null)
        {
            return;
        }

        var items = GetCategoryItems(GetCategoryKey(item.CategoryId))
            .Where(x => !ReferenceEquals(x, item))
            .OrderBy(x => x.Position)
            .ThenBy(x => x.CreatedDate)
            .ToList();

        items.Insert(Math.Clamp(position, 0, items.Count), item);
        Renumber(items);
    }

    // Incoming items win at the limit (AC 1.3.5): per category, the source's newest MaxItems come over, then the
    // target's newest items fill what is left. A product with the same configuration keeps the target's item.
    public virtual void Merge(CompareList source)
    {
        foreach (var group in source.Items.GroupBy(x => GetCategoryKey(x.CategoryId), StringComparer.OrdinalIgnoreCase))
        {
            var incomingItems = group
                .OrderByDescending(x => x.CreatedDate)
                .Take(MaxItems)
                .OrderBy(x => x.Position)
                .ToList();

            var mergedItems = new List<CompareListItem>();

            foreach (var sourceItem in incomingItems)
            {
                var item = FindItem(sourceItem.ProductId, sourceItem.ConfigurationKey);

                if (item == null && List.Items.Count < ModuleConstants.MaxListItems)
                {
                    item = CopyItem(sourceItem);
                    List.Items.Add(item);
                }

                if (item != null)
                {
                    mergedItems.Add(item);
                }
            }

            TrimCategory(group.Key, mergedItems);
        }
    }

    protected virtual void TrimCategory(string categoryKey, IList<CompareListItem> keptItems)
    {
        var otherItems = GetCategoryItems(categoryKey)
            .Where(x => !keptItems.Any(keptItem => ReferenceEquals(keptItem, x)))
            .OrderByDescending(x => x.CreatedDate)
            .ToList();

        var removedItems = otherItems.Skip(Math.Max(0, MaxItems - keptItems.Count)).ToHashSet(ReferenceEqualityComparer.Instance);

        List.Items = List.Items.Where(x => !removedItems.Contains(x)).ToList();

        Renumber(GetCategoryItems(categoryKey).OrderBy(x => x.Position).ThenBy(x => x.CreatedDate).ToList());
    }

    protected virtual CompareListItem CopyItem(CompareListItem sourceItem)
    {
        var item = AbstractTypeFactory<CompareListItem>.TryCreateInstance();
        item.ProductId = sourceItem.ProductId;
        item.CategoryId = sourceItem.CategoryId;
        item.ConfigurationKey = sourceItem.ConfigurationKey;
        item.Position = GetNextPosition(sourceItem.CategoryId);
        item.Sections = sourceItem.Sections
            .Select(x =>
            {
                var section = x.CloneTyped();
                section.Id = null;
                return section;
            })
            .ToList();

        return item;
    }

    protected virtual CompareListItemSection ToSection(ProductConfigurationSection section)
    {
        var result = AbstractTypeFactory<CompareListItemSection>.TryCreateInstance();
        result.SectionId = section.SectionId;
        result.Type = section.Type;
        result.ProductId = section.Option?.ProductId;
        result.Quantity = section.Option?.Quantity;
        result.CustomText = section.CustomText;

        return result;
    }

    // Two items are the same when the product and every section's choice match, as the storefront compared them.
    protected virtual string GetConfigurationKey(IList<ProductConfigurationSection> sections)
    {
        if (sections.IsNullOrEmpty())
        {
            return string.Empty;
        }

        var normalized = string.Join('\n', sections
            .OrderBy(x => x.SectionId, StringComparer.Ordinal)
            .Select(x => $"{x.SectionId}|{x.Type}|{x.Option?.ProductId}|{x.Option?.Quantity}|{x.CustomText}"));

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    protected virtual CompareListItem FindItem(string productId, string configurationKey)
    {
        return List.Items.FirstOrDefault(x => x.ProductId.EqualsIgnoreCase(productId) && x.ConfigurationKey == configurationKey);
    }

    protected virtual int GetNextPosition(string categoryId)
    {
        var items = GetCategoryItems(GetCategoryKey(categoryId));

        return items.Count == 0 ? 0 : items.Max(x => x.Position) + 1;
    }

    protected virtual IList<CompareListItem> GetCategoryItems(string categoryKey)
    {
        return List.Items.Where(x => GetCategoryKey(x.CategoryId).EqualsIgnoreCase(categoryKey)).ToList();
    }

    protected static string GetCategoryKey(string categoryId)
    {
        return categoryId ?? string.Empty;
    }

    private static void Renumber(IList<CompareListItem> items)
    {
        for (var i = 0; i < items.Count; i++)
        {
            items[i].Position = i;
        }
    }
}
