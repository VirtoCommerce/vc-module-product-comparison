using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.ProductComparison.Core;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;

/// <summary>
/// The store module, as the compare list reads it: a store's id, default language and currency, and its settings.
/// Every store carries the module's store-level settings without a value, as the store module loads them for a
/// store that saved none, so each reads its descriptor default until a test sets it (<see cref="SetSetting"/>).
/// An id that isn't a known store finds nothing, as with the real service.
/// </summary>
internal sealed class TestStoreService : IStoreService
{
    private readonly ConcurrentDictionary<string, Store> _stores = new(StringComparer.OrdinalIgnoreCase);

    public Store Add(string storeId)
    {
        var store = new Store
        {
            Id = storeId,
            Name = storeId,
            DefaultLanguage = "en-US",
            DefaultCurrency = "USD",
            Catalog = "catalog",
            Settings = ModuleConstants.Settings.StoreLevelSettings
                .Select(descriptor => new ObjectSettingEntry(descriptor) { ObjectType = nameof(Store), ObjectId = storeId })
                .ToList(),
        };

        _stores[storeId] = store;

        return store;
    }

    public void SetSetting(string storeId, SettingDescriptor descriptor, object value)
    {
        _stores[storeId].Settings.Single(x => x.Name == descriptor.Name).Value = value;
    }

    public Task<IList<Store>> GetAsync(IList<string> ids, string responseGroup = null, bool clone = true)
    {
        IList<Store> stores = ids
            .Select(id => _stores.GetValueOrDefault(id))
            .Where(x => x != null)
            .Select(x => clone ? (Store)x.Clone() : x)
            .ToList();

        return Task.FromResult(stores);
    }

    public Task<IList<Store>> GetByOuterIdsAsync(IList<string> outerIds, string responseGroup = null, bool clone = true) => throw new NotSupportedException();

    public Task SaveChangesAsync(IList<Store> models) => throw new NotSupportedException();

    public Task DeleteAsync(IList<string> ids, bool softDelete = false) => throw new NotSupportedException();

    public Task<IList<string>> GetUserAllowedStoreIdsAsync(ApplicationUser user) => throw new NotSupportedException();
}
