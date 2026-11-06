using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;

/// <summary>
/// The requests of one type the module sends through MediatR, recorded on their way to the real handler, which still
/// answers them: for a test about the module's own calls, such as how many queries a loader sends, which the answer
/// alone doesn't show.
/// </summary>
internal sealed class SentRequests<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public ConcurrentQueue<TRequest> Requests { get; } = new();

    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        Requests.Enqueue(request);

        return next();
    }
}
