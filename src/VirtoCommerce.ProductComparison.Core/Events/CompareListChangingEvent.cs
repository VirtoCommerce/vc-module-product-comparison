using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.ProductComparison.Core.Models;

namespace VirtoCommerce.ProductComparison.Core.Events;

public class CompareListChangingEvent : GenericChangedEntryEvent<CompareList>
{
    public CompareListChangingEvent(IEnumerable<GenericChangedEntry<CompareList>> changedEntries)
        : base(changedEntries)
    {
    }
}
