using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FluentAssertions;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;

/// <summary>A serialized GraphQL response (data and errors), as a client receives it.</summary>
internal sealed class GraphQlResponse
{
    public GraphQlResponse(string json)
    {
        Json = json;

        using var document = JsonDocument.Parse(json);
        Root = document.RootElement.Clone();
    }

    public string Json { get; }

    public JsonElement Root { get; }

    /// <summary>The <c>extensions.code</c> of every error, in order; empty when the call succeeded.</summary>
    public IList<string> ErrorCodes => Root.TryGetProperty("errors", out var errors)
        ? errors.EnumerateArray()
            .Select(error => error.TryGetProperty("extensions", out var extensions) && extensions.TryGetProperty("code", out var code) ? code.GetString() : null)
            .ToList()
        : [];

    /// <summary>The code of the response's only error: what a client reads to tell a refusal apart.</summary>
    public string ErrorCode
    {
        get
        {
            ErrorCodes.Should().ContainSingle("the call should be refused with one error: {0}", Json);

            return ErrorCodes[0];
        }
    }

    /// <summary>A root field's data, after asserting the response carries no errors.</summary>
    public JsonElement Data(string field)
    {
        Root.TryGetProperty("errors", out _).Should().BeFalse("the response should carry no errors: {0}", Json);

        return Root.GetProperty("data").GetProperty(field);
    }
}
