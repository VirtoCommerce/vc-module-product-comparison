using System;
using System.Threading.Tasks;
using FluentAssertions;
using VirtoCommerce.Platform.Core.DistributedLock;
using VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;
using Xunit;
using static VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure.CompareListTestData;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests;

/// <summary>
/// The cleanup's lock (VCST-5918). Two platform instances can fire the same occurrence of the recurring job, and a run
/// can outlast the interval to the next one: the job's handler takes a lock first, waits up to 10 seconds for the run
/// that holds it, then skips the occurrence and leaves the work to the next. The lock is the one the platform
/// registers without Redis, in process and not reentrant, so the test holding it is any other run holding it.
/// </summary>
/// <remarks>
/// A class of its own, apart from <see cref="CleanupCompareListsJobComponentTests"/>: its test waits out those 10
/// seconds, and a class's tests run one after another, while the classes run side by side.
/// </remarks>
[Trait("Category", "Component")]
public class CleanupCompareListsJobLockComponentTests
{
    // The handler's lock (a private constant): what a run holds while it cleans up.
    private const string LockResource = "product-comparison:job:cleanup-compare-lists";

    [Fact]
    public async Task ARunWhileAnotherHoldsTheLock_IsSkipped_AndTheNextOneCleansUp()
    {
        await using var ctx = await ProductComparisonTestContext.CreateAsync();
        var expired = await ctx.SaveAsync(GuestList("guest-expired"));
        await ctx.SetDatesAsync(expired.Id, modifiedDate: DateTime.UtcNow.AddDays(-3));

        await using (var held = await ctx.GetRequiredService<IDistributedLock>().TryAcquireAsync(LockResource, cancellationToken: TestContext.Current.CancellationToken))
        {
            held.Should().NotBeNull("no run holds the lock before the test takes it");

            await ctx.RunCleanupJobAsync();

            (await ctx.ReloadAsync(expired.Id)).Should().NotBeNull("a run that couldn't take the lock deletes nothing");
        }

        // Control: with the lock free, the next occurrence deletes the list the skipped one left.
        await ctx.RunCleanupJobAsync();

        (await ctx.ReloadAsync(expired.Id)).Should().BeNull();
    }
}
