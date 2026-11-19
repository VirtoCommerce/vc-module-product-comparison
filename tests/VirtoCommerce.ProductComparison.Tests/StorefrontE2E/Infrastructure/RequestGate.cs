using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;

/// <summary>
/// One turn at a time for whatever touches the harness. Its in-memory SQLite database lives on one connection, which
/// is not thread-safe, while the storefront sends its requests in parallel. The host puts its requests behind the
/// gate; a test's own reads and seeds take a turn as well.
/// </summary>
internal sealed class RequestGate : IDisposable
{
    private readonly SemaphoreSlim _turn = new(1, 1);

    /// <summary>Makes every request that reaches this point of the pipeline wait for its turn.</summary>
    public void Use(IApplicationBuilder app)
        => app.Use(async (context, next) =>
        {
            await _turn.WaitAsync(context.RequestAborted);
            try
            {
                await next(context);
            }
            finally
            {
                _turn.Release();
            }
        });

    /// <summary>Runs test-side work between requests.</summary>
    public async Task<T> RunAsync<T>(Func<Task<T>> work)
    {
        await _turn.WaitAsync();
        try
        {
            return await work();
        }
        finally
        {
            _turn.Release();
        }
    }

    public void Dispose() => _turn.Dispose();
}
