using System;
using System.IO;
using System.Linq;
using VirtoCommerce.ProductComparison.Tests.Infrastructure;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;

/// <summary>
/// The compare UI as the storefront gets it in production: the module's Module Federation plugin, which
/// <c>corepack yarn build</c> in <c>StorefrontApp</c> writes to <c>plugins/vc-frontend</c>. The tests serve that build
/// as it is. A missing build, or one older than a source under <c>StorefrontApp/src</c>, fails the group with the
/// command to run: a rebuild per run would cost half a minute, and testing a stale build would test yesterday's
/// plugin. Specs and their mocks are not part of the bundle, so they don't count; other build inputs
/// (<c>plugin.config.ts</c>, <c>vite.config.ts</c>, <c>package.json</c>, the facade) are not checked.
/// </summary>
internal static class StorefrontPlugin
{
    /// <summary>The module's id (its module.manifest), under which its settings and its plugin reach the storefront.</summary>
    public const string ModuleId = "VirtoCommerce.ProductComparison";

    /// <summary>The platform's static-file route for the module's plugin; AppManifestService builds its URLs on it.</summary>
    public const string RequestPath = $"/modules/$({ModuleId})/plugins/vc-frontend";

    public static string BuildDirectory => Path.Combine(TestedModule.Directory, "plugins", "vc-frontend");

    private static string AppDirectory => Path.Combine(TestedModule.Directory, "StorefrontApp");

    public static void EnsureBuilt()
    {
        // Written on every build, so its time is the build's.
        var build = new FileInfo(Path.Combine(BuildDirectory, "mf-manifest.json"));
        if (!build.Exists)
        {
            throw new InvalidOperationException($"The storefront plugin is not built: run `corepack yarn build` in {AppDirectory}.");
        }

        var sources = new DirectoryInfo(Path.Combine(AppDirectory, "src"));
        var newest = sources
            .EnumerateFiles("*", SearchOption.AllDirectories)
            .Where(x => !x.Name.Contains(".test.") && !IsUnder(x, Path.Combine(sources.FullName, "mocks")))
            .MaxBy(x => x.LastWriteTimeUtc);

        if (newest != null && newest.LastWriteTimeUtc > build.LastWriteTimeUtc)
        {
            throw new InvalidOperationException(
                $"The storefront plugin build ({build.LastWriteTime:g}) is older than {Path.GetRelativePath(AppDirectory, newest.FullName)} " +
                $"({newest.LastWriteTime:g}): run `corepack yarn build` in {AppDirectory}.");
        }
    }

    private static bool IsUnder(FileInfo file, string directory)
        => file.FullName.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
}
