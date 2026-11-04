using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;

/// <summary>The store's sign-in schemes, which the store query reports: the password scheme, active.</summary>
internal sealed class PasswordOnlyStoreAuthenticationService : IStoreAuthenticationService
{
    public Task<IList<StoreAuthenticationScheme>> GetStoreSchemesAsync(string storeId, bool clone = true)
        => Task.FromResult<IList<StoreAuthenticationScheme>>([new StoreAuthenticationScheme { Name = "Password", IsActive = true }]);

    public Task SaveStoreSchemesAsync(string storeId, IList<StoreAuthenticationScheme> models) => Task.CompletedTask;
}
