using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.ProductComparison.Core.Models;

namespace VirtoCommerce.ProductComparison.Core.Services;

public interface ICompareListSearchService : ISearchService<CompareListSearchCriteria, CompareListSearchResult, CompareList>
{
}
