# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Tech stack

- .NET SDK **10.0.100** (see `global.json`, `src/Build/Grand.Common.props` — `TargetFramework=net10.0`). The README still says .NET 9 — trust `global.json`/the props file.
- MongoDB 4.0+ (primary database; connection string lives in `App_Data/Settings.cfg` after install).
- .NET Aspire workload (required to build/restore — `dotnet workload install aspire`).
- ASP.NET Core MVC + Razor, AutoMapper, MediatR, FluentValidation, Vue.js for some admin/store-front widgets (`src/Web/Grand.Web/vueapp`, `webpack.config.js`).
- Central package management via `Directory.Packages.props`.

## Common commands

Run from the repo root (Windows PowerShell — chain with `;` not `&&`).

```powershell
# One-time
dotnet workload install aspire

# Restore / build
dotnet restore .\GrandNode.sln
dotnet build   .\GrandNode.sln --configuration Release

# Run the storefront (Grand.Web is the main entry point)
dotnet run --project .\src\Web\Grand.Web\Grand.Web.csproj

# Run with Aspire orchestration (spins up MongoDB container + Grand.Web)
dotnet run --project .\src\Aspire\Aspire.AppHost\Aspire.AppHost.csproj

# MongoDB via Docker (if not using Aspire)
docker run -d -p 27017:27017 --name mongodb mongo

# All tests
dotnet test .\GrandNode.sln

# Single test project
dotnet test .\src\Tests\Grand.Business.Catalog.Tests\Grand.Business.Catalog.Tests.csproj

# Single test by name filter
dotnet test .\src\Tests\Grand.Business.Catalog.Tests\Grand.Business.Catalog.Tests.csproj --filter "FullyQualifiedName~ProductServiceTests.GetProductById"

# Vue/JS assets for Grand.Web
cd .\src\Web\Grand.Web; npm install; npx webpack
```

CI (`.github/workflows/aspnetcore.yml`) runs each test project individually after starting a MongoDB container — useful as a reference for the canonical test command list.

## Architecture

The solution is a modular monolith organized in onion-style layers under `src/`. Dependencies flow inward: `Web` → `Modules`/`Business` → `Core`. Plugins, modules, and Roslyn scripts are discovered and composed at startup.

### Layer map (`src/`)

- **Core/** — foundational layer, no business logic.
  - `Grand.SharedKernel` — pure cross-cutting primitives (`CommonPath`, `SettingsConstants`, extension helpers).
  - `Grand.Domain` — POCO domain entities (Product, Customer, Order, …) grouped per aggregate.
  - `Grand.Data` — data abstractions + MongoDB/LiteDB providers (`DataSettingsManager`, `IRepository<T>`).
  - `Grand.Mapping` — AutoMapper profile base type (`IAutoMapperProfile`).
  - `Grand.Infrastructure` — startup composition root (`StartupBase`), plugin/module/Roslyn loaders, caching, type search, model binding, config classes (`AppConfig`, `PerformanceConfig`, etc.).
- **Business/** — domain services split by bounded context: `Catalog`, `Checkout`, `Customers`, `Marketing`, `Messages`, `Cms`, `Storage`, `Authentication`, `Common`. `Grand.Business.Core` holds shared interfaces/contracts that the others implement; consumers depend on `Core` only.
- **Modules/** — opt-in feature modules loaded via `ModuleLoader`. Each is a self-contained area: `Grand.Module.Api` (REST API w/ JWT, CQRS handlers in `Commands/`+`Queries/`), `Grand.Module.Installer` (first-run DB seeding), `Grand.Module.Migration`, `Grand.Module.ScheduledTasks`.
- **Plugins/** — payment/shipping/tax/auth/widget/theme integrations (Stripe, BrainTree, Facebook auth, Slider widget, Modern theme, …). Each plugin is its own project that drops compiled output under `Grand.Web/Plugins/` and is discovered at runtime by `PluginManager` based on `App_Data/InstalledPlugins.cfg`.
- **Web/** — presentation hosts. `Grand.Web` is the storefront and primary entry point. Admin/store/vendor backoffices live alongside: `Grand.Web.Admin`, `Grand.Web.Store`, `Grand.Web.Vendor`. Shared web infra: `Grand.Web.Common` (startup hooks, controllers base, filters), `Grand.Web.AdminShared`, `Grand.SharedUIResources`.
- **Aspire/** — `Aspire.AppHost` (orchestrator that wires Mongo + Grand.Web), `Aspire.ServiceDefaults` (telemetry/health defaults consumed via `builder.AddServiceDefaults()` in each host).
- **Tests/** — one MSTest project per Business module + per Core library. Most tests assume a running MongoDB on `localhost:27017`.

### Startup composition (read this before adding cross-cutting features)

`src/Web/Grand.Web/Program.cs` is intentionally thin: it delegates everything to `Grand.Infrastructure.StartupBase.ConfigureServices` and `ConfigureRequestPipeline`. The interesting work happens inside `StartupBase` (`src/Core/Grand.Infrastructure/StartupBase.cs`):

1. `TypeSearcher` scans loaded assemblies (Business, Modules, Plugins, Roslyn-compiled scripts) for known interfaces.
2. `ModuleLoader.LoadModules` + `PluginManager.Load` + `RoslynCompiler.Load` register additional MVC application parts so controllers/views inside modules/plugins are picked up.
3. AutoMapper profiles (`IAutoMapperProfile`), type converters (`ITypeConverter`), validator consumers (`IValidatorConsumer<>`), and MediatR handlers are discovered and registered from every assembly.
4. `IStartupApplication` implementations are discovered repo-wide and invoked in two passes (`BeforeConfigure` then after) ordered by `Priority` — this is the extension point for adding services, middleware, or endpoints from any layer.
5. `IStartupBase` implementations run last for one-shot bootstrap work.

When adding new functionality that needs DI/middleware wiring, prefer implementing `IStartupApplication` in the appropriate layer rather than editing `Program.cs` or `StartupBase`.

### Cross-cutting patterns

- **CQRS via MediatR**: Business operations are typically `IRequest`/`IRequestHandler` pairs. The API module (`Grand.Module.Api`) exposes thin controllers that send commands/queries.
- **Multi-store / multi-tenant**: `IStoreContext` and `IWorkContext` (in `Grand.Infrastructure`) carry the active store and customer; most services depend on these rather than `HttpContext`.
- **Plugin gating**: `PluginExtensions.OnlyInstalledPlugins` filters discovered types so uninstalled plugin assemblies don't contribute services/profiles/startups even when their DLLs are on disk.
- **Configuration**: strongly-typed config (`AppConfig`, `SecurityConfig`, `CacheConfig`, `RedisConfig`, `BackendAPIConfig`, `FrontendAPIConfig`, …) is bound from `appsettings.json` sections inside `RegisterConfigurations`. Optional Azure App Configuration is supported via the `Azure:AppConfiguration` key.
- **Runtime Roslyn scripts**: `src/Web/Grand.Web/Roslyn` and the `RoslynCompiler` allow `.cs` files to be compiled and merged into the MVC part manager at startup for customization without rebuilding the solution.

### Where to make common changes

- New domain entity → `Grand.Domain/<Area>` + repository usage from a `Grand.Business.<Area>` service.
- New service interface → declare in `Grand.Business.Core`, implement in the matching `Grand.Business.<Area>` project, register via an `IStartupApplication`.
- New admin UI → `Grand.Web.Admin` (controller + Razor view + nav entry); for a new "module-style" backoffice area see `Grand.Web.Store` / `Grand.Web.Vendor` as templates.
- New REST endpoint → add command/query + handler in `Grand.Module.Api/Commands` or `/Queries`, then a thin controller in `Grand.Module.Api/Controllers`.
- New payment/shipping/tax/widget → copy the structure of an existing project under `src/Plugins/` and register the plugin folder in `App_Data/Plugins.cfg` (created during install).

## Version note

`Grand.Common.props` pins the assembly version at `2.4.0`. The git-describe target stamps revision/branch metadata into assemblies at build time.
