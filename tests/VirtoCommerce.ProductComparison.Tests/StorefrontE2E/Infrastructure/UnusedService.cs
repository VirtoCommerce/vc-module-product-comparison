using System;
using System.Reflection;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;

/// <summary>
/// A constructor dependency the storefront's pages never reach. The profile graph types under the page context's
/// user (contact, organization) take customer-module services the harness doesn't host; the E2E buyers have no
/// contact, so those fields resolve to null and the services are never called. Should one be, the call fails with
/// the service's name instead of a null reference somewhere inside a resolver.
/// </summary>
internal class UnusedService : DispatchProxy
{
    private Type _serviceType;

    public static T Of<T>() where T : class
    {
        var proxy = Create<T, UnusedService>();
        ((UnusedService)(object)proxy)._serviceType = typeof(T);

        return proxy;
    }

    protected override object Invoke(MethodInfo targetMethod, object[] args)
        => throw new NotSupportedException(
            $"{_serviceType.Name}.{targetMethod?.Name} was called, but the storefront E2E backend does not host it: " +
            "a page now reaches a part of the profile the harness leaves out.");
}
