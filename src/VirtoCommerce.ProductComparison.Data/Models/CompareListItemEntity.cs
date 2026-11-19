using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.ProductComparison.Core.Models;

namespace VirtoCommerce.ProductComparison.Data.Models;

public class CompareListItemEntity : AuditableEntity, IDataEntity<CompareListItemEntity, CompareListItem>
{
    public const int ConfigurationKeyLength = DbContextBase.Length64;

    [Required]
    [StringLength(DbContextBase.IdLength)]
    public string CompareListId { get; set; }

    public virtual CompareListEntity CompareList { get; set; }

    [Required]
    [StringLength(DbContextBase.IdLength)]
    public string ProductId { get; set; }

    [StringLength(DbContextBase.IdLength)]
    public string CategoryId { get; set; }

    // "" for a product without configuration, so the unique (list, product, key) index also covers plain products.
    [Required]
    [StringLength(ConfigurationKeyLength)]
    public string ConfigurationKey { get; set; }

    public int Position { get; set; }

    public virtual ObservableCollection<CompareListItemSectionEntity> Sections { get; set; } = new NullCollection<CompareListItemSectionEntity>();

    public virtual CompareListItem ToModel(CompareListItem model)
    {
        ArgumentNullException.ThrowIfNull(model);

        model.Id = Id;
        model.CreatedBy = CreatedBy;
        model.CreatedDate = CreatedDate;
        model.ModifiedBy = ModifiedBy;
        model.ModifiedDate = ModifiedDate;
        model.ProductId = ProductId;
        model.CategoryId = CategoryId;
        model.ConfigurationKey = ConfigurationKey;
        model.Position = Position;
        model.Sections = Sections
            .OrderBy(x => x.SectionId)
            .Select(x => x.ToModel(AbstractTypeFactory<CompareListItemSection>.TryCreateInstance()))
            .ToList();

        return model;
    }

    public virtual CompareListItemEntity FromModel(CompareListItem model, PrimaryKeyResolvingMap pkMap)
    {
        ArgumentNullException.ThrowIfNull(model);

        pkMap.AddPair(model, this);

        Id = model.Id;
        CreatedBy = model.CreatedBy;
        CreatedDate = model.CreatedDate;
        ModifiedBy = model.ModifiedBy;
        ModifiedDate = model.ModifiedDate;
        ProductId = model.ProductId;
        CategoryId = model.CategoryId;
        ConfigurationKey = model.ConfigurationKey;
        Position = model.Position;

        if (model.Sections != null)
        {
            Sections = new ObservableCollection<CompareListItemSectionEntity>(model.Sections.Select(x => AbstractTypeFactory<CompareListItemSectionEntity>.TryCreateInstance().FromModel(x, pkMap)));
        }

        return this;
    }

    public virtual void Patch(CompareListItemEntity target)
    {
        target.ProductId = ProductId;
        target.CategoryId = CategoryId;
        target.ConfigurationKey = ConfigurationKey;
        target.Position = Position;

        if (!Sections.IsNullCollection())
        {
            Sections.Patch(target.Sections, (sourceSection, targetSection) => sourceSection.Patch(targetSection));
        }
    }
}
