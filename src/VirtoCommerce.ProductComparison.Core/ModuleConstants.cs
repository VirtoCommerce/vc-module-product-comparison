using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ProductComparison.Core;

public static class ModuleConstants
{
    public const int MaxListItems = 500;

    public const int CustomTextLength = 255;

    public static class Settings
    {
        public static class General
        {
            public static SettingDescriptor Enabled { get; } = new()
            {
                Name = "ProductComparison.Enabled",
                GroupName = "Product Comparison|General",
                ValueType = SettingValueType.Boolean,
                DefaultValue = true,
                IsPublic = true,
            };

            public static SettingDescriptor MaxItems { get; } = new()
            {
                Name = "ProductComparison.MaxItems",
                GroupName = "Product Comparison|General",
                ValueType = SettingValueType.PositiveInteger,
                DefaultValue = 5,
                IsPublic = true,
            };

            public static SettingDescriptor AnonymousListEnabled { get; } = new()
            {
                Name = "ProductComparison.AnonymousListEnabled",
                GroupName = "Product Comparison|General",
                ValueType = SettingValueType.Boolean,
                DefaultValue = true,
                IsPublic = true,
            };

            public static IEnumerable<SettingDescriptor> AllGeneralSettings
            {
                get
                {
                    yield return Enabled;
                    yield return MaxItems;
                    yield return AnonymousListEnabled;
                }
            }
        }

        public static class Cleanup
        {
            public static SettingDescriptor Enabled { get; } = new()
            {
                Name = "ProductComparison.CleanupAnonymousLists.Enabled",
                GroupName = "Product Comparison|Cleanup",
                ValueType = SettingValueType.Boolean,
                DefaultValue = true,
            };

            public static SettingDescriptor Cron { get; } = new()
            {
                Name = "ProductComparison.CleanupAnonymousLists.Cron",
                GroupName = "Product Comparison|Cleanup",
                ValueType = SettingValueType.ShortText,
                DefaultValue = "0 2 * * *",
            };

            public static SettingDescriptor AnonymousListLifetimeHours { get; } = new()
            {
                Name = "ProductComparison.AnonymousListLifetimeHours",
                GroupName = "Product Comparison|Cleanup",
                ValueType = SettingValueType.PositiveInteger,
                DefaultValue = 48,
            };

            public static IEnumerable<SettingDescriptor> AllCleanupSettings
            {
                get
                {
                    yield return Enabled;
                    yield return Cron;
                    yield return AnonymousListLifetimeHours;
                }
            }
        }

        public static IEnumerable<SettingDescriptor> AllSettings => General.AllGeneralSettings.Concat(Cleanup.AllCleanupSettings);

        public static IEnumerable<SettingDescriptor> StoreLevelSettings => General.AllGeneralSettings;
    }
}
