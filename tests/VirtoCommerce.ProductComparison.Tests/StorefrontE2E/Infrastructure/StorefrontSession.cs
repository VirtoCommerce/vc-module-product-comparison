using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;

/// <summary>
/// A browser context and its page. Records what the storefront said while the test ran (console errors, dialogs,
/// failed requests, the GraphQL operations it sent and those that returned errors), and on a failure folds that plus
/// a screenshot into the exception, so a red test explains itself without re-running under a debugger.
/// </summary>
internal sealed class StorefrontSession : IAsyncDisposable
{
    private readonly List<string> _console = [];
    private readonly List<string> _dialogs = [];
    private readonly List<string> _operations = [];
    private readonly List<string> _graphQlErrors = [];
    private readonly List<string> _failedRequests = [];

    public StorefrontSession(IBrowserContext context, IPage page)
    {
        Context = context;
        Attach(page);
    }

    /// <summary>The browser context: one storage (localStorage) for every page the test opens in it.</summary>
    public IBrowserContext Context { get; }

    public IPage Page { get; private set; }

    /// <summary>The names of the GraphQL operations the storefront sent so far, in order.</summary>
    public IReadOnlyList<string> Operations
    {
        get
        {
            lock (_operations)
            {
                return [.. _operations];
            }
        }
    }

    /// <summary>The GraphQL operations that came back with errors so far, one line each.</summary>
    public IReadOnlyList<string> GraphQlErrors
    {
        get
        {
            lock (_graphQlErrors)
            {
                return [.. _graphQlErrors];
            }
        }
    }

    /// <summary>A full page load. Vite compiles on first request, so with a clone the run's first navigation is the slow one.</summary>
    public async Task GotoAsync(string path)
    {
        Page.SetDefaultTimeout(30_000);
        await Page.GotoAsync(path, new PageGotoOptions { Timeout = 120_000, WaitUntil = WaitUntilState.Load });
    }

    /// <summary>
    /// Closes the page and loads <paramref name="path"/> in a new one of the same context, which keeps the
    /// localStorage: the storefront's next visit in the same browser. A second full load in the same page can leave a
    /// clone's page blank (its module requests fail with net::ERR_INSUFFICIENT_RESOURCES or are aborted), so the visit
    /// gets a page of its own.
    /// </summary>
    public async Task OpenInNewPageAsync(string path)
    {
        await Page.CloseAsync();
        Attach(await Context.NewPageAsync());
        await GotoAsync(path);
    }

    /// <summary>Runs the test body; a failure is rethrown with the storefront's own account of what happened.</summary>
    public async Task RunAsync(string name, Func<IPage, Task> body)
    {
        try
        {
            await body(Page);
        }
        catch (Exception exception)
        {
            var report = await ReportAsync(name);
            throw new StorefrontTestException($"{exception.Message}{Environment.NewLine}{report}", exception);
        }
    }

    /// <summary>A value the page keeps in its localStorage, or null: what the storefront and the plugin remember between loads.</summary>
    public Task<string> GetLocalStorageAsync(string key)
        => Page.EvaluateAsync<string>("key => localStorage.getItem(key)", key);

    private void Attach(IPage page)
    {
        Page = page;

        page.Console += (_, message) =>
        {
            if (message.Type is "error" or "warning")
            {
                Record(_console, $"[{message.Type}] {message.Text}");
            }
        };

        page.Dialog += (_, dialog) =>
        {
            Record(_dialogs, $"[{dialog.Type}] {dialog.Message}");
            _ = dialog.AcceptAsync();
        };

        page.Request += (_, request) =>
        {
            if (IsGraphQl(request))
            {
                Record(_operations, GetOperationName(request));
            }
        };

        page.RequestFailed += (_, request) => Record(_failedRequests, $"{request.Method} {request.Url}: {request.Failure}");

        page.Response += (_, response) => _ = RecordGraphQlErrorsAsync(response);
    }

    private async Task<string> ReportAsync(string name)
    {
        var report = new StringBuilder();
        var directory = Path.Combine(AppContext.BaseDirectory, "StorefrontE2E-diagnostics");

        try
        {
            Directory.CreateDirectory(directory);
            var screenshot = Path.Combine(directory, $"{name}.png");
            await Page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshot, FullPage = true });
            report.AppendLine($"Screenshot: {screenshot}");
        }
        catch (Exception screenshotError)
        {
            report.AppendLine($"Screenshot failed: {screenshotError.Message}");
        }

        report.AppendLine($"URL: {Page.Url}");
        report.AppendLine($"Title: {await SafeAsync(() => Page.TitleAsync())}");
        report.AppendLine($"Body (start): {Truncate(await SafeAsync(() => Page.InnerTextAsync("body")), 800)}");
        Append(report, "GraphQL operations", _operations);
        Append(report, "GraphQL errors", _graphQlErrors);
        Append(report, "Dialogs", _dialogs);
        Append(report, "Failed requests", _failedRequests);
        Append(report, "Console", _console);

        return report.ToString();
    }

    private async Task RecordGraphQlErrorsAsync(IResponse response)
    {
        try
        {
            if (!IsGraphQl(response.Request))
            {
                return;
            }

            var body = await response.TextAsync();
            if (!body.Contains("\"errors\""))
            {
                return;
            }

            Record(_graphQlErrors, $"{GetOperationName(response.Request)} ({response.Status}): {Truncate(body, 600)}");
        }
        catch
        {
            // Diagnostics only.
        }
    }

    private static bool IsGraphQl(IRequest request) => request.Url.Contains("/graphql") && request.Method == "POST";

    private static string GetOperationName(IRequest request)
    {
        try
        {
            if (request.PostData is { } postData)
            {
                using var document = JsonDocument.Parse(postData);
                if (document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("operationName", out var operation))
                {
                    return operation.GetString();
                }
            }
        }
        catch (JsonException)
        {
            // Not a JSON body: no name to read.
        }

        return "?";
    }

    private static void Record(List<string> target, string line)
    {
        lock (target)
        {
            target.Add(line);
        }
    }

    private static void Append(StringBuilder report, string title, List<string> lines)
    {
        lock (lines)
        {
            report.AppendLine($"{title}: {(lines.Count == 0 ? "none" : string.Empty)}");
            foreach (var line in lines.Take(30))
            {
                report.AppendLine($"  {line}");
            }
        }
    }

    private static async Task<string> SafeAsync(Func<Task<string>> read)
    {
        try
        {
            return await read();
        }
        catch (Exception exception)
        {
            return $"<{exception.Message}>";
        }
    }

    private static string Truncate(string value, int length)
        => value == null ? string.Empty : value.Length <= length ? value : value[..length] + "…";

    public async ValueTask DisposeAsync() => await Context.CloseAsync();
}
