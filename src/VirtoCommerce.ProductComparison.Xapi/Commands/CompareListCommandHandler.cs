using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.ProductComparison.Xapi.Aggregates;

namespace VirtoCommerce.ProductComparison.Xapi.Commands;

public abstract class CompareListCommandHandler<TCommand>(ICompareListAggregateRepository repository)
    : IRequestHandler<TCommand, CompareListAggregate>
    where TCommand : CompareListCommand
{
    // Only a save creates the list; the other commands have nothing to change before it exists.
    protected virtual bool CreatesList => false;

    public virtual async Task<CompareListAggregate> Handle(TCommand request, CancellationToken cancellationToken)
    {
        var aggregate = await repository.GetAsync(request);
        aggregate.EnsureAvailable();

        if (!aggregate.IsSaved && !CreatesList)
        {
            return null;
        }

        Update(aggregate, request);
        await repository.SaveAsync(aggregate);

        return aggregate;
    }

    protected abstract void Update(CompareListAggregate aggregate, TCommand request);
}
