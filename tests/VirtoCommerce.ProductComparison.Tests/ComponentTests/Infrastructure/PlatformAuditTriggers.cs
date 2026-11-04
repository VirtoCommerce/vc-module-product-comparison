using System;
using System.Runtime.CompilerServices;
using System.Threading;
using EntityFrameworkCore.Triggers;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;

/// <summary>
/// The audit stamping the platform adds at startup (Platform.Data's <c>UseDbTriggers</c>): an inserted row gets
/// CreatedDate = ModifiedDate = now, an updated row keeps its CreatedDate and gets ModifiedDate = now. Without it a
/// list saved through the module would keep the model's empty dates, and the cleanup, which reads ModifiedDate,
/// would see lists no deployment ever has.
/// </summary>
/// <remarks>
/// The triggers are process-wide statics, so they are added once, at assembly load, never per test. Only the
/// module's own DbContext runs in this process, so nothing else is stamped.
/// </remarks>
internal static class PlatformAuditTriggers
{
    public const string UserName = "component-test";

    private static int _registered;

    [ModuleInitializer]
    internal static void Register()
    {
        if (Interlocked.Exchange(ref _registered, 1) != 0)
        {
            return;
        }

        Triggers<IAuditable>.Inserting += entry =>
        {
            var currentTime = DateTime.UtcNow;

            entry.Entity.CreatedDate = currentTime;
            entry.Entity.ModifiedDate = currentTime;
            entry.Entity.CreatedBy = UserName;
            entry.Entity.ModifiedBy = UserName;
        };

        Triggers<IAuditable>.Updating += entry =>
        {
            entry.Entity.CreatedDate = entry.Original.CreatedDate;
            entry.Entity.CreatedBy = entry.Original.CreatedBy;
            entry.Entity.ModifiedDate = DateTime.UtcNow;
            entry.Entity.ModifiedBy = UserName;
        };
    }
}
