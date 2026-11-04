using VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.GuestCompare;

/// <summary>
/// Group 1: a store with the module's defaults (compare on, guests' lists on, five products per category) and the
/// shared catalog. Every test brings its own guest, and the sign-in test its own buyer, so nothing is shared but the
/// catalog.
/// </summary>
public sealed class GuestCompareEnvironment : StorefrontEnvironment;
