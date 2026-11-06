using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.ProductComparison.Core.Models;

namespace VirtoCommerce.ProductComparison.Data.Models;

public class CompareListEntity : AuditableEntity, IDataEntity<CompareListEntity, CompareList>
{
    [Required]
    [StringLength(DbContextBase.IdLength)]
    public string StoreId { get; set; }

    [Required]
    [StringLength(DbContextBase.IdLength)]
    public string UserId { get; set; }

    [StringLength(DbContextBase.IdLength)]
    public string OrganizationId { get; set; }

    public bool IsAnonymous { get; set; }

    public virtual ObservableCollection<CompareListItemEntity> Items { get; set; } = new NullCollection<CompareListItemEntity>();

    public virtual CompareList ToModel(CompareList model)
    {
        ArgumentNullException.ThrowIfNull(model);

        model.Id = Id;
        model.CreatedBy = CreatedBy;
        model.CreatedDate = CreatedDate;
        model.ModifiedBy = ModifiedBy;
        model.ModifiedDate = ModifiedDate;
        model.StoreId = StoreId;
        model.UserId = UserId;
        model.OrganizationId = OrganizationId;
        model.IsAnonymous = IsAnonymous;
        model.Items = Items
            .OrderBy(x => x.Position)
            .ThenBy(x => x.CreatedDate)
            .Select(x => x.ToModel(AbstractTypeFactory<CompareListItem>.TryCreateInstance()))
            .ToList();

        return model;
    }

    public virtual CompareListEntity FromModel(CompareList model, PrimaryKeyResolvingMap pkMap)
    {
        ArgumentNullException.ThrowIfNull(model);

        pkMap.AddPair(model, this);

        Id = model.Id;
        CreatedBy = model.CreatedBy;
        CreatedDate = model.CreatedDate;
        ModifiedBy = model.ModifiedBy;
        ModifiedDate = model.ModifiedDate;
        StoreId = model.StoreId;
        UserId = model.UserId;
        OrganizationId = model.OrganizationId;
        IsAnonymous = model.IsAnonymous;

        if (model.Items != null)
        {
            Items = new ObservableCollection<CompareListItemEntity>(model.Items.Select(x => AbstractTypeFactory<CompareListItemEntity>.TryCreateInstance().FromModel(x, pkMap)));
        }

        return this;
    }

    public virtual void Patch(CompareListEntity target)
    {
        target.StoreId = StoreId;
        target.UserId = UserId;
        target.OrganizationId = OrganizationId;
        target.IsAnonymous = IsAnonymous;

        if (!Items.IsNullCollection())
        {
            Items.Patch(target.Items, (sourceItem, targetItem) => sourceItem.Patch(targetItem));
        }
    }
}
