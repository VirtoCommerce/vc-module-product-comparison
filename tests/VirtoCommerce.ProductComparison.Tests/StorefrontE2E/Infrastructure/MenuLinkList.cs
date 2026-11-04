using System.Collections.Generic;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;

internal sealed class MenuLinkList
{
    public string Name { get; set; }

    public IList<MenuLink> Items { get; set; } = [];
}
