using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;

/// <summary>
/// X-Cart's product configuration query, as the compare list's section loader sends it: the sections a configurable
/// product has now, answered for the section ids asked about. Every query is recorded, so a test can check that the
/// loader sends one query per product.
/// </summary>
internal sealed class TestProductConfigurationHandler : IRequestHandler<GetProductConfigurationQuery, ProductConfigurationQueryResponse>
{
    private readonly ConcurrentDictionary<string, IList<ExpProductConfigurationSection>> _sections = new(StringComparer.OrdinalIgnoreCase);

    public ConcurrentQueue<GetProductConfigurationQuery> Queries { get; } = new();

    public void Add(string productId, params (string Id, string Name)[] sections)
    {
        _sections[productId] = sections.Select(x => new ExpProductConfigurationSection { Id = x.Id, Name = x.Name }).ToList();
    }

    public Task<ProductConfigurationQueryResponse> Handle(GetProductConfigurationQuery request, CancellationToken cancellationToken)
    {
        Queries.Enqueue(request);

        var sections = _sections.GetValueOrDefault(request.ConfigurableProductId) ?? [];

        return Task.FromResult(new ProductConfigurationQueryResponse
        {
            ConfigurationSections = sections.Where(x => request.SectionIds.Contains(x.Id, StringComparer.OrdinalIgnoreCase)).ToList(),
        });
    }
}
