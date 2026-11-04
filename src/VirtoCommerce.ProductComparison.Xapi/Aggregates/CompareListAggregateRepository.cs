using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.ProductComparison.Core;
using VirtoCommerce.ProductComparison.Core.Models;
using VirtoCommerce.ProductComparison.Core.Services;
using VirtoCommerce.ProductComparison.Xapi.Models;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.ProductComparison.Xapi.Aggregates;

public class CompareListAggregateRepository(
    ICompareListService compareListService,
    ICompareListSearchService compareListSearchService,
    IStoreService storeService)
    : ICompareListAggregateRepository
{
    public virtual async Task<CompareListAggregate> GetAsync(ICompareListRequest request)
    {
        var store = await storeService.GetNoCloneAsync(request.StoreId) ?? throw CompareListErrors.StoreNotFound(request.StoreId);

        var criteria = AbstractTypeFactory<CompareListSearchCriteria>.TryCreateInstance();
        criteria.StoreId = request.StoreId;
        criteria.UserId = request.UserId;
        criteria.OrganizationId = request.OrganizationId;
        criteria.OrganizationIdIsEmpty = string.IsNullOrEmpty(request.OrganizationId);
        // A guest reaches only guest lists: a buyer's id in another case passes the guest rule where ids compare
        // case-sensitively, and the search cache, which ignores case, would answer with the buyer's list.
        criteria.IsAnonymous = request.IsAnonymous;
        criteria.Take = 1;

        var list = (await compareListSearchService.SearchAsync(criteria)).Results.FirstOrDefault() ?? CreateList(request);

        var aggregate = AbstractTypeFactory<CompareListAggregate>.TryCreateInstance();
        aggregate.List = list;
        aggregate.Store = store;
        aggregate.CultureName = request.CultureName ?? store.DefaultLanguage;
        aggregate.CurrencyCode = request.CurrencyCode ?? store.DefaultCurrency;
        aggregate.IsEnabled = store.Settings.GetValue<bool>(ModuleConstants.Settings.General.Enabled);
        aggregate.IsAnonymousListEnabled = store.Settings.GetValue<bool>(ModuleConstants.Settings.General.AnonymousListEnabled);
        aggregate.MaxItems = store.Settings.GetValue<int>(ModuleConstants.Settings.General.MaxItems);

        return aggregate;
    }

    public virtual Task<CompareList> GetListByIdAsync(string listId)
    {
        return compareListService.GetByIdAsync(listId);
    }

    public virtual Task SaveAsync(CompareListAggregate aggregate)
    {
        return compareListService.SaveChangesAsync([aggregate.List]);
    }

    public virtual Task DeleteListAsync(CompareList list)
    {
        return compareListService.DeleteAsync([list.Id]);
    }

    protected virtual CompareList CreateList(ICompareListRequest request)
    {
        var list = AbstractTypeFactory<CompareList>.TryCreateInstance();
        list.StoreId = request.StoreId;
        list.UserId = request.UserId;
        list.OrganizationId = request.OrganizationId;
        list.IsAnonymous = request.IsAnonymous;

        return list;
    }
}
