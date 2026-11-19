using System;
using System.Linq;
using FluentAssertions;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProductComparison.Xapi;
using VirtoCommerce.ProductComparison.Xapi.Commands;
using VirtoCommerce.ProductComparison.Xapi.Queries;
using VirtoCommerce.Xapi.Core.Infrastructure;
using Xunit;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests;

/// <summary>
/// The compare list's gate (validation, the account-state check, the token's organization and the
/// <c>CanAccessCompareList</c> requirement) lives in two builders: <c>CompareListCommandBuilder&lt;,&gt;</c>, the base
/// of every mutation, and <c>CompareListQueryBuilder</c>. It protects an endpoint only if the endpoint is one of
/// them, so a new builder that derives from the X-API's own bases instead would expose lists unchecked.
/// </summary>
[Trait("Category", "Component")]
public class CompareListEndpointGateTests
{
    [Fact]
    public void EverySchemaBuilder_GoesThroughTheGate()
    {
        // Scanned rather than resolved from DI: this also catches a builder that exists but was never registered,
        // and needs no harness.
        var builders = typeof(XapiAssemblyMarker).Assembly.GetTypes()
            .Where(x => !x.IsAbstract && typeof(ISchemaBuilder).IsAssignableFrom(x))
            .ToList();

        builders.Should().NotBeEmpty("the module exposes its endpoints as schema builders");

        var ungated = builders
            .Where(x => !IsGated(x))
            .Select(x => x.Name)
            .ToList();

        ungated.Should().BeEmpty("every compare list endpoint must be CompareListQueryBuilder or derive from CompareListCommandBuilder<,>");
    }

    private static bool IsGated(Type type)
    {
        return typeof(CompareListQueryBuilder).IsAssignableFrom(type)
            || type.GetTypeInheritanceChain().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(CompareListCommandBuilder<,>));
    }
}
