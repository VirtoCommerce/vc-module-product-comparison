using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;

/// <summary>
/// The module-level settings (the cleanup reads its lifetime from here). A setting a test didn't set reports no
/// value, which is how <c>GetValueAsync</c> falls back to the descriptor default, so tests get the module's real
/// defaults unless they say otherwise. Registration is a no-op: nothing here lists settings.
/// </summary>
internal sealed class TestSettingsManager : ISettingsManager
{
    public ConcurrentDictionary<string, object> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<SettingDescriptor> AllRegisteredSettings => [];

    public void RegisterSettings(IEnumerable<SettingDescriptor> settings, string moduleId = null)
    {
    }

    public void RegisterSettingsForType(IEnumerable<SettingDescriptor> settings, string typeName)
    {
    }

    public IEnumerable<SettingDescriptor> GetSettingsForType(string typeName) => [];

    public IDictionary<string, string[]> GetSettingTypeAssignments() => new Dictionary<string, string[]>();

    public Task<ObjectSettingEntry> GetObjectSettingAsync(string name, string objectType = null, string objectId = null)
    {
        return Task.FromResult(new ObjectSettingEntry { Name = name, Value = Values.GetValueOrDefault(name) });
    }

    public async Task<IEnumerable<ObjectSettingEntry>> GetObjectSettingsAsync(IEnumerable<string> names, string objectType = null, string objectId = null)
    {
        return await Task.WhenAll(names.Select(name => GetObjectSettingAsync(name, objectType, objectId)));
    }

    public Task SaveObjectSettingsAsync(IEnumerable<ObjectSettingEntry> objectSettings) => throw new NotSupportedException();

    public Task RemoveObjectSettingsAsync(IEnumerable<ObjectSettingEntry> objectSettings) => throw new NotSupportedException();
}
