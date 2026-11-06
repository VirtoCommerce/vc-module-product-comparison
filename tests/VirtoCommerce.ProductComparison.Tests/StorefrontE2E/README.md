# Storefront E2E

Playwright tests that drive the **real storefront** with the module's **compare plugin** against the **component
harness hosted inside the test process**. No second dotnet process: the same `ProductComparisonTestContext` (the module
as the platform starts it, among the real platform and modules around it, every database an in-memory SQLite one) sits
behind Kestrel on a loopback port, with the modules behind the storefront's shell besides, so a test seeds through the
real services and mutations, acts through the browser and asserts against the same rows.

```
test process ─┬─ Kestrel (InProcessBackend) ── ProductComparisonTestContext: the module + the platform (settings,
              │                                  accounts, dynamic properties, change log) + store, core, customer,
              │                                  catalog, pricing, inventory, marketing, cart, payment, shipping and
              │                                  assets modules (SQLite) + search (Lucene, RAM) + X-API, X-Catalog, X-Cart
              │                                + X-Profile, X-Frontend, SEO, X-CMS + content, X-Recommend (the shell's)
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

## Platform and modules

The backend runs the platform's and the modules' own code, as a deployment runs it; the test's own code is only the
sign-in token endpoint and the list of installed modules (see Doubles below).
- **Data:** every module and the platform keep their data in an in-memory SQLite database of their own
  (`ModuleDatabases`), created from their EF model when first used.
- **Search:** the search module's gateway serves the Lucene provider with its index in RAM.
- **Registrations:** the platform's own extensions where it has them (Platform.Data's `AddPlatformServices`: settings,
  dynamic properties, the change log, the caches, the event bus; its `UseDbTriggers`, once per process: the audit
  stamps), and Platform.Web's `Startup` restated where it is the host's own (ASP.NET Identity with the platform's user
  and role managers, authorization, the HTTP client factory, Kestrel's synchronous IO). Each module registers what its
  `Module.Initialize` registers at the package version referenced here, minus export and import, bulk actions,
  background jobs, other modules' event handlers and admin authorization (`PlatformModules` for what the component
  tests share, `StorefrontShellModules` for the shell's). X-API, X-Catalog and X-Cart register through their own
  extensions (`AddXCore`, `AddSchema` + `AddXCatalog`, `AddSchema` + `AddXCart`).
- **Configuration:** what the platform's shipped `appsettings.json` sets that this code reads: the default store, the
  GraphQL playground (on), the authentication types GraphQL ignores; and the platform's country lists, which it ships in
  its own content root, as copies in `Platform/localization`.
- **Startup:** the module's own `Module.PostInitialize`, and what the other modules' `PostInitialize` does: their
  settings (the platform refuses to read a setting nobody registered), dynamic property and member types, search
  request builders, the prices and stock in the product index (the category page filters on them: no price, no
  result), the payment and shipping methods, and the GraphQL endpoints.
- **Seed:** what an administrator would set up, through the modules' services: a USD currency and the stores
  (`ProductComparisonTestContext`), then the catalog (`TestCatalog`): the store's catalog, a pricelist assigned to the
  store, and categories (with an SEO entry each, which breadcrumbs need), products and prices. The index is then built
  with the search module's indexing manager, a full rebuild. The product page, the compare list's products and tabs,
  and the category page's cards are X-Catalog's answers from that index; an "Add to cart" goes through X-Cart's
  `addItem` into the cart module's tables. A buyer is what the storefront's registration creates: a contact and its
  account, in the store.

Where a module ships provider-specific SQL (SQL Server, PostgreSQL, MySQL), the harness runs the module's PostgreSQL
statements on SQLite: `SqliteCatalogRawDatabaseCommand` (a category's children and parents, a product's associations,
the deletes of products and categories) and `SqliteCartRawDatabaseCommand` (the wishlists that hold a page's
products). Statements nothing here reaches throw with their name instead.

Blob storage is the file system providers' (assets and content), over temporary folders: nothing seeded has a file.
The tax module is not installed, as on a store without it: X-Catalog and X-Cart take it as optional.

### Doubles

Two pieces stand in for the platform's:
- **Tokens:** the platform's OpenIddict server isn't hosted. `TestTokenService` mints a bearer token with the claims a
  customer's token carries, for an account of the security database, and the test endpoint answers the storefront's
  refresh grant and revoke.
- **Installed modules:** the platform builds that list by discovering module folders at startup.
  `StorefrontModuleService` reports the module (from its real `module.manifest`) and X-Frontend, the app whose plugins
  `store.plugins` lists.

## Layout

| Piece | File | Role |
|---|---|---|
| Backend host | `Infrastructure/InProcessBackend.cs` | Copies the harness registrations into a `WebApplication`, starts Kestrel on port 0 (with a clone, on the port the run's Vite proxies to), builds the schema eagerly, self-checks the `store` query and its plugin; `RunExclusiveAsync` gives test-side reads and seeds a turn between requests. |
| Shell services | `Infrastructure/StorefrontServices.cs` | The shell's modules, the platform's `AppManifestService` for `store.plugins` over the module list, bearer authentication for the token, Kestrel's synchronous IO, and the request pipeline before the modules' endpoints: a built storefront's files, the plugin's files, one request at a time, authentication. |
| Shell modules | `Infrastructure/StorefrontShellModules.cs` | X-Profile (`me`, `organization`, `currentCustomerAddresses`), X-Frontend (`pageContext`) with the SEO module behind its slug, X-CMS over the content module's menus (`menu`), X-Recommend (`searchHistory`), registered and started as their modules do (see Platform and modules). |
| Platform and modules | `../Infrastructure/`: `PlatformModules*.cs`, `ModuleDatabases.cs`, `SqlCommandLog.cs`, `PlatformDbTriggers.cs`, `TestedModule.cs`, `Sqlite*RawDatabaseCommand.cs`; `../Platform/localization` | Shared with the component tests: the platform's and the modules' registrations and startup steps, their SQLite databases and the SQL they run, the platform's audit stamps, the module's own manifest, the raw SQL on SQLite, the country lists (see Platform and modules). |
| Catalog | `../Infrastructure/TestCatalog.cs`, `Infrastructure/E2ECatalog.cs` | The seed, through the modules' services, and the index rebuild (see Platform and modules); what every group gets: two categories under two top ones, three plain products with prices, and a product id the catalog never had. |
| Doubles | `Infrastructure/StorefrontModuleService.cs`; the Tokens row | The module list (the module's own `module.manifest` plus X-Frontend, which declares the storefront app, at a version the storefront's "outdated backend" check can't flag) and the tokens (see Doubles). |
| Compare plugin | `Infrastructure/StorefrontPlugin.cs` | The module's plugin build (`plugins/vc-frontend`) and its route; a missing build, or one older than its sources under `StorefrontApp/src` (specs and `src/mocks` aside), fails the group. |
| Tokens | `Infrastructure/TestTokenService.cs`, `TokenResponse.cs`, `TestTokenEndpoints.cs` | Bearer tokens with the claims a customer's token carries (`sub`, `name`, `email`, `organization_id` when given), stored the way `useAuth.ts` stores them; the refresh grant and `/revoke/token`. |
| Storefront location | `Infrastructure/StorefrontLocation.cs` | Finds the storefront, a clone or a build (see Running); a missing or broken one fails the groups with what to do. `StorefrontLocationTests.cs` covers the lookup (Unit). |
| Vite | `Infrastructure/ViteDevServer.cs`, `Infrastructure/SharedViteDevServer.cs` | A clone's dev server, one per run, on a free port of its own: `node node_modules/vite/bin/vite.js --port N --strictPort` with `APP_BACKEND_URL`, which is fixed at start, so each group's backend takes that port in turn; stopped by an xunit assembly fixture once every test has run; bounded kill. |
| Group fixture | `Infrastructure/StorefrontEnvironment.cs` | Backend + store settings + catalog + browser per xunit collection; `OpenAsGuestAsync(guestId, path)` = a fresh browser context as a guest the storefront knows by that id; `NewGuestId`, `NewBuyerAsync` give a test a guest or a buyer of its own (a contact and its account), `IssueTokenAsync` signs the buyer in; `SaveAsGuestAsync` and `SaveAsBuyerAsync` seed a guest's or a buyer's list through the real mutation; `GetListsAsync` reads `CompareList`/`CompareListItem` rows between requests, `GetCartsAsync` a user's carts through the cart module; `ConfigureStoreAsync` for a group's store settings, saved as an administrator saves them, `SeedAsync` for its own products and lists, before the index is built; phase log. |
| Request gate | `Infrastructure/RequestGate.cs` | One turn at a time: the host's requests wait at it, and test-side work takes a turn through `RunExclusiveAsync`. |
| Session | `Infrastructure/StorefrontSession.cs` | Records console errors, dialogs, failed requests, the GraphQL operations sent and those that returned errors; a failing test gets them plus a screenshot in its message. `OpenInNewPageAsync` is the next visit in the same browser (same localStorage); `GetLocalStorageAsync` reads what the storefront remembers; `GetGraphQlErrorsAsync` gives the operations that came back with errors once the body of every response the page got so far has been read, so a check for none can't run ahead of a response the page already acted on, and a body it couldn't read counts as an error; `GetOperationName` names a routed request's operation. |
| Controls | `Infrastructure/CompareControls.cs` | The compare toggle in the price block or a catalog card, the header's Compare and Cart links and their counts, the toasts and their Compare link, and the compare page's tabs, columns and buttons, by role and label. |
| Layout audit | `Infrastructure/LayoutAudit.cs` | QA's audit widths and overflow audit (rule BL-UI-004) for the layout tests: prepares a page for measuring, walks the widths, and lists every width and element that broke (see Layout tests). A copy of vc-module-sales-rep's, with this module's namespace and the class prefix that names a culprit's plugin element (`compare-`). |
| Documents | `Infrastructure/StorefrontDocuments.cs` | Loads the storefront's own `.graphql` operations from the clone and the plugin's from `StorefrontApp` (fragments inlined, `@gate` applied). |
| Contract | `StorefrontBootContractTests.cs` | Browser-less: the backend answers the storefront's boot documents, the product page's `GetProduct`, a signed-in buyer's `GetCurrentUserAddresses` and the plugin's own operations, for a guest and a buyer (a few seconds). |
| Group 1 | `GuestCompare/` | A store with the module's defaults. `GuestCompareTests`: a guest adds from the product page, the header counts it, the compare page shows it under its category, removing it empties page, count and list; a guest adds from a card on the category page; a full category refuses a sixth product before any request; "Add to cart" from the compare page puts the product in the guest's cart; the add's toast has a "Compare" link to the page, open on the product's category; a "No longer available" column for a product the catalog doesn't have, and its remove; "Clear category" with its confirmation empties that category alone; "Clear all" with its confirmation, then "Restore products". `SignInMergeTests`: a guest's list merges into the buyer's on the first signed-in load, the guest list is deleted and the remembered id forgotten; a merge that fails on its way (aborted, or a gateway's 502 page) keeps the id, and the next load merges; a merge into an account list at the 500-product cap is refused with `COMPARE_LIST_FULL`, both lists stay as they were, the id is forgotten and the buyer is told the list is full. Each asserted on the page and on the rows. |
| Group 2 | `GuestListsOff/` | `ProductComparison.AnonymousListEnabled` off: a guest's click gets "Sign in to add products to compare" with a Sign in link back to the page (AC 3); nothing is sent, no list exists. |
| Group 3 | `CompareOff/` | `ProductComparison.Enabled` off: no toggle and no slot kept for it on the product page, no header link, the plugin never runs (no `CompareList` sent), and `/compare` is the storefront's 404. |
| Group 4 | `CompareLayout/` | The compare page's layout (`Kind=Layout`), with QA's stress content: an 80-character category name, a 100-character product name, two names that differ only at their ends, one with no break in it, an unbroken SKU and model number, long notes, big amounts, a product the catalog no longer has, property rows enough to scroll the page and categories enough to fill the tab row. The page as it opens, with the differences shown and a row pinned, scrolled until the table's header sticks, with the Clear category and the Clear all dialog open, and emptied, each at QA's 19 widths; and that the plugin's stylesheets select only its own elements (see Layout tests). |

Groups are xunit collections with `DisableParallelization = true`: each runs alone, so one backend and one browser
exist at a time, and a clone's one Vite serves them in turn. Tests inside a group share the environment, its store and
its catalog; each test gets its own browser context (fresh localStorage, fresh Apollo cache) and its own guest, and the
merge test its own buyer, so what a test saves is nobody else's. Nothing is cleaned up: the database and the catalog
live in memory and go with the group. Each test carries a 5-minute timeout.

Measured on a developer workstation. Against a build (the vc-frontend PR's), the whole run takes about a minute and a
half: a group's backend, with its modules, seed and index, is up in a few seconds, the run's first browser test takes
longer and the others a second or two, and a layout test, which walks its 19 widths, about five. Against a clone it
takes about three minutes: the run's first page load costs about twenty seconds while Vite compiles the storefront on
demand, and every page after it loads slower than a build's.

## Running

The tests run wherever they find a storefront, and anywhere else they fail with what is missing. The module CI never
runs them: every class also carries the `IntegrationTest` category, which `vc-build Test` leaves out by default. In
GitHub, the `E2E tests` workflow runs them on demand against any vc-frontend branch (see the module README).

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
dotnet test --filter "Category=StorefrontE2E"                # contract + every storefront group, layout included
dotnet test --filter "Category!=IntegrationTest"             # everything else, as vc-build Test runs it
E2E_HEADED=1 dotnet test --filter "Category=StorefrontE2E"   # watch the browser
```

The layout tests can also run apart from the functional ones (see Layout tests).

Diagnostics land in `bin/Debug/net10.0/StorefrontE2E-diagnostics/`: `environment-*.log` (timestamped phases per
group), `vite-<port>.log` (the run's dev server output, with a clone), `<TestName>.png` (full-page screenshot of a
failed test).

## Layout tests

Group 4 checks what fits, not what works. Its class carries `Kind=Layout` besides the two categories every group
carries, so a run can pick the tests by kind. The `E2E tests` workflow runs them all.

```
dotnet test --filter "Category=StorefrontE2E"                # functional and layout, as the workflow runs them
dotnet test --filter "Kind=Layout"                           # layout only, e.g. after a style change
dotnet test --filter "Category=StorefrontE2E&Kind!=Layout"   # functional only
```

One test checks the plugin's CSS rather than a width. A plugin's stylesheets apply to the whole storefront page, load
after the storefront's own CSS and stay for the visit, so each of their selectors must name one of the plugin's BEM
blocks (`compare-table`, `compare-products`): one that doesn't restyles the storefront. A plain `.flex-col` from the
plugin's own copy of Tailwind's utilities once beat the storefront's `lg:flex-row` and stacked the sign-up form's
fields on a desktop. A failure lists every selector that strays.

The others each open one state of the compare page and walk QA's audit widths: every ui-kit breakpoint (480, 640, 768,
1024, 1280, 1500), the pixel below it, the middles between them, and 375 and 1920 (ai-tools
`scripts/lib/design-tokens.generated.ts`, `AUDIT_VIEWPORTS_PX`). At each width it applies QA's overflow audit, rule
BL-UI-004 (ai-tools `scripts/lib/measure-layout.ts`):
- the page never scrolls sideways: the document's scroll width stays within its client width;
- no text inside the page or its open dialog is cut off by `overflow: hidden` without an ellipsis (`text-overflow` or
  `line-clamp`). Text for screen readers only, and boxes with no text, don't count.

The compare table scrolls sideways inside its own frame by design: what a frame scrolls (`overflow: auto`) is neither
cut off nor a reason for the page to scroll.

A failure lists every width and element at once, for example `375 px: the page scrolls sideways, 711 px of content in
375: p.compare-products__empty-description in section.vc-widget.vc-widget--size--md “Add products from the catalog to
compare” ends at 712`, and leaves screenshots of the first three widths that failed as `<test>-<width>px.png` in the
diagnostics folder.

What keeps the results the same from run to run:
- the group seeds its own data, and the browser is the Chromium build Playwright pins;
- the checks compare each element with its own box, never with a pixel value;
- before measuring, the page loses its transitions and animations;
- the storefront's own font (Lato, from Google Fonts) must have loaded. A fallback font changes the width of every text,
  and the fallbacks differ by OS, so a font that did not load fails the test rather than measuring something else;
- after each resize the audit waits until the page's size holds still.

Not checked: the product page's action row and the header's Compare link (the theme's, around the plugin's toggle and
count), touch targets and their spacing at phone widths (BL-UI-006), German labels, a mobile browser context (the tests
resize a desktop one), and whether text that fits is also readable: two look-alike names cut with an ellipsis pass.

## Harness changes

- `ProductComparisonTestContext.CreateAsync(configureServices, buildHost)` lets a host build (and own) the web
  application the platform's and the modules' startup then adds its endpoints to.
- The test project references Microsoft.Playwright, the platform's packages (Platform.Modules for `AppManifestService`
  and optional dependencies, Platform.Security and Platform.Data through the modules), the data packages of the store,
  core, customer, catalog, pricing, inventory, marketing, cart, payment, shipping, assets (with the file system
  provider), file experience API, search, Lucene, SEO and content (with its file system provider) modules, and of
  X-API, X-Catalog, X-Cart, X-Profile, X-Frontend, X-CMS and X-Recommend, at the versions X-Cart.Core 3.1042.0 and
  X-Catalog.Core 3.1023.0 depend on (X-Frontend at 3.1008.0, the last one on this platform), and the ASP.NET Core
  framework (Kestrel).

## What the run taught

- The storefront learns a guest's id from the page context and sends it as the compare list's `userId`. A shell that
  answers a guest without an id with an empty one (as sales-rep's did, which only signs reps in) makes every compare
  call fail closed, so the shell does what X-Frontend does: it keeps the id the storefront sent, or makes a new one,
  never a registered account's.
- The module's store settings reach the storefront only under the module's id, which the platform's settings
  registrar stamps on the descriptors when the module's `PostInitialize` registers them. The harness runs that
  `PostInitialize`: drop its store-settings registration and the contract test and the compare-off group fail.
- The plugin build found on disk was older than two of its sources (`useCompareList.ts`, `compare-products.vue`):
  `EnsureBuilt` catches that. Specs and `src/mocks` are vitest-only, so they don't count as sources of the bundle.
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
- On the platform modules, the contract test named what the paths reach, one at a time, as the profile types had:
  - the payment and shipping methods: the cart service filters a cart's payments and shipments by them on every load;
  - the file upload service: X-Cart hands it a cart's configuration files on every save, an empty list here;
  - inventory: X-Cart's cart product service takes it as a hard dependency;
  - X-API's one type registration: X-Catalog creates prices through the factory with their currency;
  - the modules' raw SQL: a category save clears its children's cache with it.
- The real validators check the seed: a pricelist goes to a store or a catalog, not both, and a property's name is
  code-like (its label is a display name).
- The category page filters out products without a price, and prices reach the index only through the pricing module's
  startup step, which adds them to the product index: without it, the page showed "0 results".
- The platform's own services showed what the doubles had papered over:
  - the settings manager refuses a setting no module registered, so every hosted module's settings are registered as
    its `PostInitialize` registers them;
  - X-API reads its options from the platform's appsettings: without `GraphQLPlayground:Enable` its scoped schemas ask
    for a GraphiQL page it never registered, and with the playground off it edits a process-wide list of validation
    rules, which parallel test classes trip over; the shipped value (on) avoids both;
  - X-API accepts a GraphQL POST only as `application/json` exactly (Apollo's), not `application/json; charset=utf-8`,
    and its Newtonsoft serializer needs Kestrel's synchronous IO, which Platform.Web allows;
  - X-API finds a module's optional graph types (X-Frontend's white labeling) only among loaded assemblies, so the
    harness loads the modules' assemblies first, as the platform does at startup;
  - the profile's address book answers an account without a contact with Forbidden, and X-Recommend's search history
    an account without a store; the storefront turns any Forbidden into its 403 page. A storefront-registered buyer has
    both, so the E2E buyers do too;
  - a real currency needs its name set: `Currency`'s own constructor sets the English name, not the one saved;
  - the catalog deletes products, and a category's products with it, through raw SQL of its own: the product-deleted
    handler's tests reached it once they deleted through the catalog instead of publishing an event of their own, so
    the SQLite port carries those deletes too.
- The platform's user manager caches an account under a lowercase key: once a buyer's account is cached, "BUYER-1"
  finds the buyer, and the guest rule refuses a guest asking with it. The component test for a guest who spells a
  buyer's id in another case now signs the buyer in again first, which drops the cached account, as any sign-in does.

## Known gaps

- The sign-in page itself is not driven: it posts the password grant to `/connect/token`, which the test endpoint
  doesn't implement (the harness's accounts have no passwords). The merge test leaves the browser as the sign-in leaves
  it (the token in `localStorage.auth`, `user-id` dropped) and loads the next page.
- Related-product cards (the product page's associations) are the same plugin component in the same `productActions`
  place as the catalog cards the tests drive; none is seeded.
- Not covered from PLAN §8: a configured product (it needs configurable product data), the refetch on tab focus, the
  mobile menu count.
- E2E buyers have a contact but no organization, so lists keyed by the token's organization (D12) are covered by the
  component tests only.
- Other modules' event handlers are not registered (change logs, event-based indexing, membership clean-up): the tests
  build the index in full and read no change log.
