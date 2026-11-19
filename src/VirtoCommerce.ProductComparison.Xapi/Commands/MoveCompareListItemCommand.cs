namespace VirtoCommerce.ProductComparison.Xapi.Commands;

public class MoveCompareListItemCommand : CompareListCommand
{
    public string ItemId { get; set; }

    public int Position { get; set; }
}
