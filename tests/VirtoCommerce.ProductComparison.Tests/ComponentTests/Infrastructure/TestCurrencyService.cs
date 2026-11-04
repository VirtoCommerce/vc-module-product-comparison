using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Currency;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;

/// <summary>
/// One currency, the harness stores' default. The compare list item type takes the service for its product's money
/// fields; the tests here don't select the product, so it only has to exist.
/// </summary>
internal sealed class TestCurrencyService : ICurrencyService
{
    public Task<IEnumerable<Currency>> GetAllCurrenciesAsync()
    {
        IEnumerable<Currency> currencies =
        [
            new Currency(Language.InvariantLanguage, "USD", "US Dollar", "$", 1m) { IsPrimary = true, RoundingPolicy = new DefaultMoneyRoundingPolicy() },
        ];

        return Task.FromResult(currencies);
    }

    public Task SaveChangesAsync(Currency[] currencies) => throw new NotSupportedException();

    public Task DeleteCurrenciesAsync(string[] codes) => throw new NotSupportedException();
}
