# Storefront E2E

Playwright tests that drive the **real storefront** with the module's **compare plugin** against the **component
harness hosted inside the test process**. No second dotnet process: the same `ProductComparisonTestContext` (the
module's own `Module.Initialize`, in-memory SQLite, hand-written doubles) sits behind Kestrel on a loopback port, so a
test seeds through the module's real mutation, acts through the browser and asserts against the same rows.

```
test process ─┬─ Kestrel (InProcessBackend) ── ProductComparisonTestContext (SQLite + doubles) + StorefrontCatalog
              ├─ Playwright → Chromium ──► a clone:  https://localhost:<port> (Vite dev server, one node process per run)
              │                            a build:  http://127.0.0.1:<kestrel port> (Kestrel serves it)
              └─ Vite proxy (a clone only): /graphql, /connect/token, /revoke/token, /modules/…/plugins/vc-frontend/
                             ──► http://127.0.0.1:<kestrel port>
```

The storefront is either a vc-frontend clone on a Vite dev server, one for the whole run, whose proxy forwards the
platform's paths to the current group's Kestrel, or a built storefront (the theme zip's `default` folder), which
Kestrel serves from its own origin, with `index.html` for page routes, the way the cloud serves it.

The compare UI reaches the storefront the way it does in production: as the module's Module Federation plugin. The
storefront asks `store.plugins` for it, the platform's own `AppManifestService` answers from the module's
`plugins/vc-frontend/plugin.json`, and Kestrel serves the build on the platform's route for it. The plugin's
`when` (`ProductComparison.Enabled`) is evaluated by the storefront against the store's settings, as served by the
real X-API `store` query.

## Product data

The storefront reads products through X-Catalog: the product page's `product` field and the compare list's `product`
field both go through `LoadProductsQuery`, the compare page's category tabs through `LoadCategoryQuery`. In production
X-Catalog answers both from the search index. Here `StorefrontCatalog` answers them from memory, at the query level:
catalog models built as the catalog module saves them (`CatalogProduct` and `Category` with outlines and per-store SEO
entries), wrapped as X-Catalog wraps them (`ExpProduct` with a list price, `ExpCategory`). Everything around those two
queries is real: X-Catalog's root fields (`DigitalCatalogSchema`), its store access rule, its graph types (name, price,
availability, breadcrumbs) and its handlers for the product page's extras (slug, outline, configuration, videos,
associations) over empty catalog searches.

Why not the real index: the catalog module over SQLite, Lucene and X-Catalog's search pipeline (its price, inventory,
discount, tax and vendor middlewares) would mostly test X-Catalog, at the cost of several more harness slices. What
the compare list needs from the catalog is that its product and category loads resolve, and that an id the catalog
doesn't have comes back missing (the "no longer available" column); the double does both the way the index does. The
breadcrumbs the plugin takes a product's compare category from are built by X-Catalog's own code from the outlines,
and the double refuses a query for a store that doesn't exist, so a wrong store id in the module's loads would show.

## Layout

| Piece | File | Role |
|---|---|---|
| Backend host | `Infrastructure/InProcessBackend.cs` | Copies the harness registrations into a `WebApplication`, starts Kestrel on port 0 (with a clone, on the port the run's Vite proxies to), builds the schema eagerly, self-checks the `store` query and its plugin; `RunExclusiveAsync` gives test-side reads and seeds a turn between requests. |
| Shell services | `Infrastructure/StorefrontServices.cs` | Real X-API `store` handler, real X-Catalog root fields and satellite handlers, real profile `UserType`, the platform's `AppManifestService` for `store.plugins`; JWT bearer auth; pipeline (a built storefront's files, plugin files, one request at a time); the store the harness double hands out, made into a storefront's (`ConfigureStore`). |
| Shell schema | `Infrastructure/StorefrontShellSchemaBuilder.cs`, plus a file per response model and per graph type: `PageContextResponse`, `MenuLinkList`, `MenuLink`, `SearchHistoryResult`, each with its `…Type` | `pageContext` and `me` (a guest keeps the id the storefront sends, as X-Frontend does; a signed-in caller gets their account), `currentCustomerAddresses` (a signed-in storefront's address book), `childCategories`, `cart`, `organization`, `menu`, `searchHistory`: real response types, empty data. |
| Catalog | `Infrastructure/StorefrontCatalog.cs`, `Infrastructure/E2ECatalog.cs`, `Infrastructure/NoCatalogExtrasSearchService.cs` | The in-memory catalog at X-Catalog's query level (see Product data); the seed every group gets: two categories under two top ones, three plain products, and a product id the catalog never had. |
| Doubles | `Infrastructure/`: `EmptyStoreSearchService.cs`, `CurrencyServiceStoreCurrencyResolver.cs`, `PasswordOnlyStoreAuthenticationService.cs`, `StorefrontModuleService.cs`, `NoDynamicPropertySearchService.cs`, `NoCartAvailMethodsService.cs`, `UnusedService.cs` | Store search, currencies and sign-in schemes; the module list (the module's own `module.manifest` plus X-Frontend, which declares the storefront app, at a version the storefront's "outdated backend" check can't flag); no dynamic properties; `UnusedService` proxies for constructor-only dependencies of the profile and cart graph types, which name themselves if a page ever calls one. |
| Compare plugin | `Infrastructure/StorefrontPlugin.cs` | The module's plugin build (`plugins/vc-frontend`) and its route; a missing build, or one older than its sources under `StorefrontApp/src` (specs and `src/mocks` aside), fails the group. |
| Tokens | `Infrastructure/TestTokenService.cs`, `TokenResponse.cs`, `TestTokenEndpoints.cs` | Bearer tokens with the claims a customer's token carries (`sub`, `name`, `email`, `organization_id` when given), stored the way `useAuth.ts` stores them; the refresh grant and `/revoke/token`. |
| Storefront location | `Infrastructure/StorefrontLocation.cs` | Finds the storefront, a clone or a build (see Running); a missing or broken one fails the groups with what to do. `StorefrontLocationTests.cs` covers the lookup (Unit). |
| Vite | `Infrastructure/ViteDevServer.cs`, `Infrastructure/SharedViteDevServer.cs` | A clone's dev server, one per run, on a free port of its own: `node node_modules/vite/bin/vite.js --port N --strictPort` with `APP_BACKEND_URL`, which is fixed at start, so each group's backend takes that port in turn; stopped by an xunit assembly fixture once every test has run; bounded kill. |
| Group fixture | `Infrastructure/StorefrontEnvironment.cs` | Backend + store settings + catalog + browser per xunit collection; `OpenAsGuestAsync(guestId, path)` = a fresh browser context as a guest the storefront knows by that id; `NewGuestId`, `NewBuyer` give a test a guest or a buyer of its own, `IssueToken` signs the buyer in; `SaveAsGuestAsync` seeds a list through the real mutation; `GetListsAsync` reads `CompareList`/`CompareListItem` rows between requests; `ConfigureStore` for a group's store settings; phase log. |
| Request gate | `Infrastructure/RequestGate.cs` | One turn at a time: the host's requests wait at it, and test-side work takes a turn through `RunExclusiveAsync`. |
| Session | `Infrastructure/StorefrontSession.cs` | Records console errors, dialogs, failed requests, the GraphQL operations sent and those that returned errors; a failing test gets them plus a screenshot in its message. `OpenInNewPageAsync` is the next visit in the same browser (same localStorage); `GetLocalStorageAsync` reads what the storefront remembers. |
| Controls | `Infrastructure/CompareControls.cs` | The compare toggle, the header's Compare link and its count, the toasts, and the compare page's tabs, columns and buttons, by role and label. |
| Documents | `Infrastructure/StorefrontDocuments.cs` | Loads the storefront's own `.graphql` operations from the clone and the plugin's from `StorefrontApp` (fragments inlined, `@gate` applied). |
| Contract | `StorefrontBootContractTests.cs` | Browser-less: the backend answers the storefront's boot documents, the product page's `GetProduct`, a signed-in buyer's `GetMyAddresses` and the plugin's own operations, for a guest and a buyer (≈2 s). |
| Group 1 | `GuestCompare/` | A store with the module's defaults. `GuestCompareTests`: a guest adds from the product page, the header counts it, the compare page shows it under its category, removing it empties page, count and list; a "No longer available" column for a product the catalog doesn't have, and its remove; "Clear all" with its confirmation, then "Restore products". `SignInMergeTests`: a guest's list merges into the buyer's on the first signed-in load, the guest list is deleted and the remembered id forgotten. Each asserted on the page and on the rows. |
| Group 2 | `GuestListsOff/` | `ProductComparison.AnonymousListEnabled` off: a guest's click gets "Sign in to add products to compare" with a Sign in link back to the page (AC 3); nothing is sent, no list exists. |
| Group 3 | `CompareOff/` | `ProductComparison.Enabled` off: no toggle and no slot kept for it on the product page, no header link, the plugin never runs (no `CompareList` sent), and `/compare` is the storefront's 404. |

Groups are xunit collections with `DisableParallelization = true`: each runs alone, so one backend and one browser
exist at a time, and a clone's one Vite serves them in turn. Tests inside a group share the environment, its store and
its catalog; each test gets its own browser context (fresh localStorage, fresh Apollo cache) and its own guest, and the
merge test its own buyer, so what a test saves is nobody else's. Nothing is cleaned up: the database and the catalog
live in memory and go with the group. Each test carries a 5-minute timeout.

Measured on the workstation that produced this draft, against a clone: the contract test and the three groups (seven
tests) run in about 70 seconds. A group's backend is up in under two seconds; the run's Vite in about five; the run's
first page load costs ≈20 seconds (Vite compiles on demand, then the storefront loads the plugin), every later one
about a second, the first page of each later group included.

## Running

The tests run wherever they find a storefront, and anywhere else they fail with what is missing. The module CI never
runs them: every class also carries the `IntegrationTest` category, which `vc-build Test` leaves out by default.

Prerequisites:
- **A storefront**, either of:
  - a **vc-frontend clone** with `node_modules` installed, run on its own Vite dev server; it needs **Node 22** on PATH;
  - a **build**: the theme zip's `default` folder, a release or a PR build, served by Kestrel. No node process.

  The tests take `VC_STOREFRONT_BUILD` (a build) or `VC_STOREFRONT_DIR` (a clone) when set; otherwise a `vc-frontend`
  clone next to this repo, then a build two levels up. In a ticket folder those are `sources\vc-frontend` and `front`,
  so nothing needs setting there, Visual Studio included. A clone that is there wins even when it cannot run, and fails
  with `yarn install`. The storefront must carry the host side of VCST-5918 (the `productActions` extension point, the
  header link count, facade 0.2.8): before it is released, that is the ticket's vc-frontend branch.
- **Plugin build:** `corepack yarn build` in `src/VirtoCommerce.ProductComparison.Web/StorefrontApp`. A missing build,
  or one older than its sources under `src`, fails the groups with that command; they never rebuild it themselves.
  Other build inputs (`plugin.config.ts`, `vite.config.ts`, `package.json`, the facade) are not checked: rebuild after
  changing them.
- **Playwright browsers** for Microsoft.Playwright 1.63: run `pwsh bin/Debug/net10.0/playwright.ps1 install chromium`
  once, if `%LOCALAPPDATA%\ms-playwright\chromium-1243` is not already there.

The contract test reads the storefront's `.graphql` files, which only a clone has. With a build alone it fails and says
so; `VC_STOREFRONT_DIR` naming the clone the build came from runs it too.

```
dotnet test --filter "Category=StorefrontE2E"                # contract + every storefront group
dotnet test --filter "Category!=IntegrationTest"             # everything else, as vc-build Test runs it
E2E_HEADED=1 dotnet test --filter "Category=StorefrontE2E"   # watch the browser
```

Diagnostics land in `bin/Debug/net10.0/StorefrontE2E-diagnostics/`: `environment-*.log` (timestamped phases per
group), `vite-<port>.log` (the run's dev server output, with a clone), `<TestName>.png` (full-page screenshot of a
failed test).

## Harness changes

- `ProductComparisonTestContext.Create(..., providerFactory)` lets a host build (and own) the provider;
  `UserIdClaimTypes` now lists `sub` first (the platform's token claim) and `NameIdentifier` (the component tests').
- The test project references Microsoft.Playwright, VirtoCommerce.Platform.Modules (`AppManifestService`),
  VirtoCommerce.ProfileExperienceApiModule.Data (`UserType`), VirtoCommerce.XCatalog.Data (`DigitalCatalogSchema` and
  the satellite handlers; it depends on X-Catalog.Core alone) and the ASP.NET Core framework (Kestrel).

## What the run taught

- The storefront learns a guest's id from the page context and sends it as the compare list's `userId`. A shell that
  answers a guest without an id with an empty one (as sales-rep's did, which only signs reps in) makes every compare
  call fail closed, so the shell does what X-Frontend does: it keeps the id the storefront sent, or makes a new one,
  never a registered account's.
- The module's store settings reach the storefront only under the module's id. The platform's settings registrar
  stamps that id on the descriptors when the module registers them (`PostInitialize`, which the harness doesn't run),
  so the store double's entries had none, and the plugin's `when` would have read "off". `ConfigureStore` stamps it.
- The plugin build found on disk was older than two of its sources (`useCompareList.ts`, `compare-products.vue`):
  `EnsureBuilt` catches that. Specs and `src/mocks` are vitest-only, so they don't count as sources of the bundle.
- The eager schema build named, one at a time, what the profile and cart graph types take in their constructors
  beyond the harness (dynamic property dictionary items, pickup locations, cart sharing, contact repository, member
  addresses, customer preferences, memberships, roles). None is reached by these pages; each is an `UnusedService`.
- The storefront's own boot, the product page and the plugin answered on the first browser run once the contract test
  passed: the browser-less contract is the cheap place to find a shell gap.
- A page can look right while one of its requests fails: a signed-in storefront loads the account's addresses through
  `me` on every page, which the shell first lacked; the page rendered and the merge ran, with a "technical issues"
  toast below. Every test now asserts its session saw no GraphQL error, which is how `me` was found.
- The same assertion caught the storefront moving that address book to `currentCustomerAddresses` (vc-frontend
  VCST-4857, on dev 2026-11-05): the shell's fields follow the storefront's dev, so a dev merge can need one more.
- A `Notifications(page)` helper doesn't compile here: a transitive package brings a `VirtoCommerce.Notifications`
  namespace. The toast region is `Toasts(page)`.
- The header's Compare link reads "1 Compare" with its badge and "Compare" without; the count is the link's name.

## Known gaps

- The sign-in page itself is not driven: it posts the password grant to `/connect/token`, which the test endpoint
  doesn't implement (the harness's accounts have no passwords). The merge test leaves the browser as the sign-in leaves
  it (the token in `localStorage.auth`, `user-id` dropped) and loads the next page.
- No catalog listing pages (they search the index): the toggle on catalog cards and related-product cards is the same
  plugin component in the same `productActions` place, but its surroundings there are not exercised.
- Not covered from PLAN §8: the per-category limit, clearing one category, a configured product (it needs X-Cart's
  configuration query and configurable product data), the refetch on tab focus, the mobile menu count.
- E2E buyers have no contact and no organization (the profile's contact and organization types take customer-module
  services the harness doesn't host), so lists keyed by the token's organization (D12) are covered by the component
  tests only.
