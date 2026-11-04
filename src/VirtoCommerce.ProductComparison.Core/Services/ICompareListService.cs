using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.ProductComparison.Core.Models;

namespace VirtoCommerce.ProductComparison.Core.Services;

public interface ICompareListService : ICrudService<CompareList>
{
    Task RemoveProductsAsync(IList<string> productIds);
}
