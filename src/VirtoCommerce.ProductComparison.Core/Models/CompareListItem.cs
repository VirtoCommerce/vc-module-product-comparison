using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ProductComparison.Core.Models;

public class CompareListItem : AuditableEntity, ICloneable
{
    public string ProductId { get; set; }

    public string CategoryId { get; set; }

    public string ConfigurationKey { get; set; }

    public int Position { get; set; }

    public IList<CompareListItemSection> Sections { get; set; } = [];

    // The list's unique index tells items of one product apart by this key: every section's choice, in section order.
    public virtual string GetConfigurationKey()
    {
        if (Sections.IsNullOrEmpty())
        {
            return string.Empty;
        }

        var normalized = string.Join('\n', Sections
            .OrderBy(x => x.SectionId, StringComparer.Ordinal)
            .Select(x => $"{x.SectionId}|{x.Type}|{x.ProductId}|{x.Quantity}|{x.CustomText}"));

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    public virtual object Clone()
    {
        var result = (CompareListItem)MemberwiseClone();
        result.Sections = Sections?.Select(x => x.CloneTyped()).ToList();
        return result;
    }
}
