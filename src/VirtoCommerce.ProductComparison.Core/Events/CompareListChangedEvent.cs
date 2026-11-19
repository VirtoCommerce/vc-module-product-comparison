using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.ProductComparison.Core.Models;

namespace VirtoCommerce.ProductComparison.Core.Events;

public class CompareListChangedEvent : GenericChangedEntryEvent<CompareList>
{
    public CompareListChangedEvent(IEnumerable<GenericChangedEntry<CompareList>> changedEntries)
        : base(changedEntries)
    {
    }
}
