# CMS Microservice — Design Spec

**Date:** 2026-05-20  
**Status:** Approved  
**Branch:** `feature/cms-microservice`

---

## Context

GrandNode2 is a modular monolith being decomposed into microservices. `Grand.Business.Cms` (Blogs, News, Knowledgebase, Pages) is a Phase 1 extraction candidate — minimal coupling, no cross-aggregate writes, pure read/write against its own MongoDB collections.

`Grand.Web.Common` currently has a compile-time dependency on `Grand.Business.Cms`. Extracting CMS as a microservice removes that dependency and follows the established `Grand.Storage.Api` / `Grand.Storage.Client` pattern.

**Success criteria:**
- `Grand.Cms.Api` runs as a standalone process; its health check returns 200
- `Grand.Web.Common.csproj` no longer references `Grand.Business.Cms`
- All `Grand.Cms.Client.Tests` pass with no running server (MockHttpMessageHandler)
- `Grand.Web` continues to work through the client (no behaviour change)
- All git operations handled by AI: branch, commits, PR description

---

## Architecture

```
Grand.Web  ──→  Grand.Cms.Client (HTTP)  ──→  Grand.Cms.Api  ──→  Grand.Business.Cms  ──→  MongoDB
              IStartupApplication              standalone            existing logic
              replaces Grand.Business.Cms      Web API
              in Grand.Web.Common.csproj
```

### New projects

| Project | Type | Location | Port |
|---|---|---|---|
| `Grand.Cms.Api` | Web API | `src/Services/Grand.Cms.Api/` | 5200 |
| `Grand.Cms.Client` | Class Library | `src/Services/Grand.Cms.Client/` | — |
| `Grand.Cms.Client.Tests` | MSTest | `src/Tests/Grand.Cms.Client.Tests/` | — |

### Modified files

| File | Change |
|---|---|
| `src/Web/Grand.Web.Common/Grand.Web.Common.csproj` | Remove `Grand.Business.Cms`, add `Grand.Cms.Client` |
| `src/Aspire/Aspire.AppHost/Program.cs` | Add `cmsApi` variable |
| `src/Aspire/Aspire.AppHost/ProjectConfiguration.cs` | Add `ConfigureGrandCmsApiProject`, update `ConfigureGrandWebProject` |
| `src/Aspire/Aspire.AppHost/Aspire.AppHost.csproj` | Add project reference to `Grand.Cms.Api` |
| `GrandNode.sln` | Add both new projects under `Services` solution folder |
| `Directory.Packages.props` | Add `RichardSzalay.MockHttp` for tests |

---

## Grand.Cms.Api

### Program.cs bootstrap (mirrors Grand.Storage.Api/Program.cs)

```csharp
// MongoDB from Aspire connection string
// DataSettingsManager.Initialize(...)
// builder.Services.AddScoped<IDatabaseContext, MongoDBContext>()
// builder.Services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>))
// builder.Services.AddSingleton<IAuditInfoProvider, ServiceAuditInfoProvider>()
// MediatR + MemoryCache + CacheConfig
// builder.Services.AddScoped<IBlogService, BlogService>()
// builder.Services.AddScoped<INewsService, NewsService>()
// builder.Services.AddScoped<IKnowledgebaseService, KnowledgebaseService>()
// builder.Services.AddScoped<IPageService, PageService>()
// builder.Services.AddControllers()
// app.MapControllers()
```

### Endpoints

**BlogController** — `GET /api/blogs`, `GET /api/blogs/{id}`, `POST /api/blogs`, `PUT /api/blogs/{id}`, `DELETE /api/blogs/{id}`

**NewsController** — `GET /api/news`, `GET /api/news/{id}`, `POST /api/news`, `PUT /api/news/{id}`, `DELETE /api/news/{id}`

**KnowledgebaseController** — Articles: 5 endpoints (`/api/knowledgebase/articles[/{id}]`), Categories: 5 endpoints (`/api/knowledgebase/categories[/{id}]`)

**PagesController** — `GET /api/pages`, `GET /api/pages/{id}`, `POST /api/pages`, `PUT /api/pages/{id}`, `DELETE /api/pages/{id}`

**Response conventions:**
- `GET` single: `200 OK` with DTO, or `404 NotFound`
- `GET` list: `200 OK` with array DTO (never 404)
- `POST`: `201 Created` with created DTO
- `PUT`: `200 OK` with updated DTO, or `404 NotFound`
- `DELETE`: `204 NoContent`, or `404 NotFound`

### DTOs

Record types in `Grand.Cms.Api/DTOs/`. One request DTO and one response DTO per entity. Fields match domain entity properties exactly (no behaviour). Examples:

```csharp
public record BlogPostDto(string Id, string Title, string Body, string MetaTitle,
    string MetaDescription, string MetaKeywords, string SeName, bool Published,
    DateTime CreatedOnUtc, IList<string> Tags);

public record InsertBlogPostRequest(string Title, string Body, string MetaTitle,
    string MetaDescription, string MetaKeywords, string SeName, bool Published,
    IList<string> Tags);
```

---

## Grand.Cms.Client

### CmsClientStartup : IStartupApplication

```csharp
Priority = 100
BeforeConfigure = false

ConfigureServices:
  baseAddress = config["services__grand-cms-api__http__0"]
             ?? config["CmsApi:BaseAddress"]
             ?? "http://localhost:5200"
  services.AddHttpClient("cms-api", c => c.BaseAddress = new Uri(baseAddress))
  services.AddMemoryCache()
  services.AddScoped<IBlogService, CmsApiBlogService>()
  services.AddScoped<INewsService, CmsApiNewsService>()
  services.AddScoped<IKnowledgebaseService, CmsApiKnowledgebaseService>()
  services.AddScoped<IPageService, CmsApiPageService>()
```

### Client services (e.g. CmsApiBlogService)

- Constructor: `IHttpClientFactory factory, IMemoryCache cache`
- `HttpClient` obtained via `factory.CreateClient("cms-api")`
- **Caching strategy:** GET-by-id and GetAll list cached with key `"cms:blog:{id}"` / `"cms:blogs:{storeId}:{page}"`, TTL 60 minutes
- **Cache invalidation:** Insert/Update/Delete remove relevant keys
- **404 handling:** `IsSuccessStatusCode` check; return `null` on 404, `throw` on other non-success
- **Mapping:** Internal `BlogPostClientDto` record → domain `BlogPost` entity via static helper

### DTOs

Internal records in `Grand.Cms.Client/DTOs/`. Match API response shapes.

---

## Test Project (Grand.Cms.Client.Tests)

**Package:** `RichardSzalay.MockHttp` (same as existing test pattern)

**TDD order per service — 3 tests minimum:**

```csharp
// Test 1: GetAll returns mapped domain entities
[TestMethod]
public async Task GetAllBlogPosts_WhenApiReturnsItems_ReturnsMappedList()
{
    var handler = new MockHttpMessageHandler();
    handler.When(HttpMethod.Get, "http://localhost/api/blogs*")
           .Respond("application/json", "[{\"id\":\"1\",\"title\":\"Hello\"}]");
    var svc = CreateService(handler);
    var result = await svc.GetAllBlogPosts("storeId", null, null, 0, 10);
    Assert.AreEqual(1, result.Count);
    Assert.AreEqual("Hello", result[0].Title);
}

// Test 2: GetById returns null on 404
[TestMethod]
public async Task GetBlogPostById_WhenNotFound_ReturnsNull()
{
    var handler = new MockHttpMessageHandler();
    handler.When("http://localhost/api/blogs/missing")
           .Respond(HttpStatusCode.NotFound);
    var svc = CreateService(handler);
    var result = await svc.GetBlogPostById("missing");
    Assert.IsNull(result);
}

// Test 3: Insert calls POST and returns entity with Id
[TestMethod]
public async Task InsertBlogPost_CallsPostAndReturnsEntityWithId()
{
    var handler = new MockHttpMessageHandler();
    handler.When(HttpMethod.Post, "http://localhost/api/blogs")
           .Respond("application/json", "{\"id\":\"abc\",\"title\":\"New\"}");
    var svc = CreateService(handler);
    var post = new BlogPost { Title = "New" };
    await svc.InsertBlogPost(post);
    Assert.AreEqual("abc", post.Id);
}
```

Repeat pattern for News (3 tests), Knowledgebase (3 tests), Pages (3 tests). Total: **12 tests minimum**.

---

## Git Workflow

```
develop  ──→  feature/cms-microservice
               Commit 1: "chore: initialize repository with microservices artifacts"
                 - CLAUDE.md
                 - .github/copilot-instructions.md
                 - .gitignore update
                 - All prior work: Gateway, Services/, IntegrationEvents, Messaging, Cart
                 - Modified: GrandNode.sln, Directory.Packages.props, Aspire files, Web.Common.csproj

               Commit 2: "test: add CMS client unit tests (red)"
                 - Grand.Cms.Client.Tests/ (failing tests)

               Commit 3: "feat: implement Grand.Cms.Api and Grand.Cms.Client"
                 - Grand.Cms.Api/ (Web API)
                 - Grand.Cms.Client/ (HTTP client services)
                 - GrandNode.sln updated
                 - Aspire wired

               Commit 4: "refactor: replace Grand.Business.Cms with Grand.Cms.Client in Web.Common"
                 - Grand.Web.Common.csproj swap

               → PR to develop
```

---

## Verification

1. `dotnet build GrandNode.sln` — clean build, no errors
2. `dotnet test src/Tests/Grand.Cms.Client.Tests/` — all 12 tests green
3. `dotnet run --project src/Aspire/Aspire.AppHost/` — `grand-cms-api` appears in Aspire dashboard
4. `curl http://localhost:5200/api/blogs` — returns `[]` (empty, no data yet)
5. `grep -r "Grand.Business.Cms" src/Web/Grand.Web.Common/` — no matches
6. `dotnet test GrandNode.sln` — full test suite still passes
