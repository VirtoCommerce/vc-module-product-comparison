using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VirtoCommerce.Platform.Core.DistributedLock;
using VirtoCommerce.Platform.Core.Jobs;

namespace VirtoCommerce.ProductComparison.Data.BackgroundJobs;

public class CleanupCompareListsJobHandler(CleanupCompareListsJob job, IDistributedLock distributedLock, ILogger<CleanupCompareListsJobHandler> logger)
    : IBackgroundJobHandler<CleanupCompareListsJobPayload>
{
    // Waits up to 10 seconds for a previous run, then skips this occurrence; the next one picks up the work.
    private const string LockResource = "product-comparison:job:cleanup-compare-lists";
    private static readonly TimeSpan _lockTimeout = TimeSpan.FromSeconds(10);

    public virtual async Task Execute(CleanupCompareListsJobPayload payload, IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        var ran = await distributedLock.TryExecuteAsync(LockResource, _ => job.Process(), _lockTimeout, cancellationToken);
        if (!ran)
        {
            logger.LogInformation("Skipped cleaning up compare lists: a previous run still holds the lock.");
        }
    }
}
