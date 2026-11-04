using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CatalogModule.Core.Events;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.ProductComparison.Core.Services;

namespace VirtoCommerce.ProductComparison.Data.Handlers;

public class ProductChangedEventHandler(ICompareListService compareListService) : IEventHandler<ProductChangedEvent>
{
    public virtual Task Handle(ProductChangedEvent message)
    {
        var deletedProductIds = message.ChangedEntries
            .Where(x => x.EntryState == EntryState.Deleted)
            .Select(x => x.OldEntry.Id)
            .ToList();

        return deletedProductIds.Count > 0
            ? compareListService.RemoveProductsAsync(deletedProductIds)
            : Task.CompletedTask;
    }
}
