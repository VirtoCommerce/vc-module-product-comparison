using System;
using System.ComponentModel.DataAnnotations;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.ProductComparison.Core;
using VirtoCommerce.ProductComparison.Core.Models;

namespace VirtoCommerce.ProductComparison.Data.Models;

public class CompareListItemSectionEntity : Entity, IDataEntity<CompareListItemSectionEntity, CompareListItemSection>
{
    public const int TypeLength = DbContextBase.Length64;

    [Required]
    [StringLength(DbContextBase.IdLength)]
    public string CompareListItemId { get; set; }

    public virtual CompareListItemEntity CompareListItem { get; set; }

    [Required]
    [StringLength(DbContextBase.IdLength)]
    public string SectionId { get; set; }

    [Required]
    [StringLength(TypeLength)]
    public string Type { get; set; }

    [StringLength(DbContextBase.IdLength)]
    public string ProductId { get; set; }

    public int? Quantity { get; set; }

    [StringLength(ModuleConstants.CustomTextLength)]
    public string CustomText { get; set; }

    public virtual CompareListItemSection ToModel(CompareListItemSection model)
    {
        ArgumentNullException.ThrowIfNull(model);

        model.Id = Id;
        model.SectionId = SectionId;
        model.Type = Type;
        model.ProductId = ProductId;
        model.Quantity = Quantity;
        model.CustomText = CustomText;

        return model;
    }

    public virtual CompareListItemSectionEntity FromModel(CompareListItemSection model, PrimaryKeyResolvingMap pkMap)
    {
        ArgumentNullException.ThrowIfNull(model);

        pkMap.AddPair(model, this);

        Id = model.Id;
        SectionId = model.SectionId;
        Type = model.Type;
        ProductId = model.ProductId;
        Quantity = model.Quantity;
        CustomText = model.CustomText;

        return this;
    }

    public virtual void Patch(CompareListItemSectionEntity target)
    {
        target.SectionId = SectionId;
        target.Type = Type;
        target.ProductId = ProductId;
        target.Quantity = Quantity;
        target.CustomText = CustomText;
    }
}
