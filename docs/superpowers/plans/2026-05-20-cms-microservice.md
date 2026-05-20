# CMS Microservice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extract Grand.Business.Cms into a standalone Grand.Cms.Api microservice consumed via Grand.Cms.Client, following the established Grand.Storage.Api pattern.

**Architecture:** Grand.Cms.Api (port 5200) is a standalone ASP.NET Core Web API backed by Grand.Business.Cms and MongoDB. Grand.Cms.Client implements the four CMS service interfaces (IBlogService, INewsService, IKnowledgebaseService, IPageService) via HTTP, replacing the compile-time reference to Grand.Business.Cms in Grand.Web.Common. Tests use MockHttpMessageHandler — no running server required.

**Tech Stack:** .NET 10, ASP.NET Core Web API, MongoDB (via Grand.Business.Cms), MediatR, IMemoryCache, RichardSzalay.MockHttp 7.0.0, MSTest, Moq, .NET Aspire

---

## File Map

**New — Grand.Cms.Api**
- `src/Services/Grand.Cms.Api/Grand.Cms.Api.csproj`
- `src/Services/Grand.Cms.Api/Program.cs`
- `src/Services/Grand.Cms.Api/ServiceAuditInfoProvider.cs`
- `src/Services/Grand.Cms.Api/appsettings.json`
- `src/Services/Grand.Cms.Api/wwwroot/.gitkeep`
- `src/Services/Grand.Cms.Api/Properties/launchSettings.json`
- `src/Services/Grand.Cms.Api/Controllers/BlogController.cs`
- `src/Services/Grand.Cms.Api/Controllers/NewsController.cs`
- `src/Services/Grand.Cms.Api/Controllers/KnowledgebaseController.cs`
- `src/Services/Grand.Cms.Api/Controllers/PagesController.cs`
- `src/Services/Grand.Cms.Api/DTOs/BlogDtos.cs`
- `src/Services/Grand.Cms.Api/DTOs/NewsDtos.cs`
- `src/Services/Grand.Cms.Api/DTOs/KnowledgebaseDtos.cs`
- `src/Services/Grand.Cms.Api/DTOs/PageDtos.cs`

**New — Grand.Cms.Client**
- `src/Services/Grand.Cms.Client/Grand.Cms.Client.csproj`
- `src/Services/Grand.Cms.Client/CmsClientConstants.cs`
- `src/Services/Grand.Cms.Client/DTOs/BlogClientDtos.cs`
- `src/Services/Grand.Cms.Client/DTOs/NewsClientDtos.cs`
- `src/Services/Grand.Cms.Client/DTOs/KnowledgebaseClientDtos.cs`
- `src/Services/Grand.Cms.Client/DTOs/PageClientDtos.cs`
- `src/Services/Grand.Cms.Client/Services/CmsApiBlogService.cs`
- `src/Services/Grand.Cms.Client/Services/CmsApiNewsService.cs`
- `src/Services/Grand.Cms.Client/Services/CmsApiKnowledgebaseService.cs`
- `src/Services/Grand.Cms.Client/Services/CmsApiPageService.cs`
- `src/Services/Grand.Cms.Client/Startup/CmsClientStartup.cs`

**New — Grand.Cms.Client.Tests**
- `src/Tests/Grand.Cms.Client.Tests/Grand.Cms.Client.Tests.csproj`
- `src/Tests/Grand.Cms.Client.Tests/Services/CmsApiBlogServiceTests.cs`
- `src/Tests/Grand.Cms.Client.Tests/Services/CmsApiNewsServiceTests.cs`
- `src/Tests/Grand.Cms.Client.Tests/Services/CmsApiKnowledgebaseServiceTests.cs`
- `src/Tests/Grand.Cms.Client.Tests/Services/CmsApiPageServiceTests.cs`

**Modified**
- `Directory.Packages.props` — add RichardSzalay.MockHttp 7.0.0
- `GrandNode.sln` — add all 3 new projects under Services/Tests folders
- `src/Aspire/Aspire.AppHost/Aspire.AppHost.csproj` — add project ref to Grand.Cms.Api
- `src/Aspire/Aspire.AppHost/ProjectConfiguration.cs` — add ConfigureGrandCmsApiProject, update ConfigureGrandWebProject
- `src/Aspire/Aspire.AppHost/Program.cs` — wire cmsApi
- `src/Web/Grand.Web.Common/Grand.Web.Common.csproj` — swap Grand.Business.Cms → Grand.Cms.Client

---

## Task 0: Repository Initialization + Feature Branch

**Files:** git operations only

- [ ] **Step 1: Commit all existing untracked work to develop**

```bash
git add CLAUDE.md .github/copilot-instructions.md .gitignore docs/
git add src/Business/Grand.Business.Core/Interfaces/Checkout/Cart/
git add src/Core/Grand.Domain/Cart/
git add src/Core/Grand.Infrastructure/Messaging/
git add src/Core/Grand.SharedKernel/IntegrationEvents/
git add src/Gateway/
git add src/Services/
git add Directory.Packages.props GrandNode.sln
git add src/Aspire/Aspire.AppHost/ src/Aspire/Aspire.ServiceDefaults/
git add src/Core/Grand.Infrastructure/StartupBase.cs
git add src/Web/Grand.Web.Common/Grand.Web.Common.csproj
git commit -m "chore: initialize repository with microservices decomposition artifacts

- Add CLAUDE.md and .github/copilot-instructions.md
- Add YARP gateway (src/Gateway/Grand.Gateway/)
- Add Grand.Storage.Api microservice (src/Services/Grand.Storage.Api/)
- Add Grand.Storage.Client HTTP wrapper (src/Services/Grand.Storage.Client/)
- Add IEventBus + InMemoryEventBus (src/Core/Grand.Infrastructure/Messaging/)
- Add integration event DTOs (src/Core/Grand.SharedKernel/IntegrationEvents/)
- Add ShoppingCart aggregate + ICartService interface
- Wire Aspire orchestration (external MongoDB, no Docker required)
- Replace Grand.Business.Storage with Grand.Cms.Client in Grand.Web.Common

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

- [ ] **Step 2: Create feature branch**

```bash
git checkout -b feature/cms-microservice
```

- [ ] **Step 3: Verify branch**

```bash
git status
```
Expected: `On branch feature/cms-microservice, nothing to commit`

---

## Task 1: Package + Project Scaffolding

**Files:**
- Modify: `Directory.Packages.props`
- Create: `src/Services/Grand.Cms.Api/Grand.Cms.Api.csproj`
- Create: `src/Services/Grand.Cms.Client/Grand.Cms.Client.csproj`
- Create: `src/Tests/Grand.Cms.Client.Tests/Grand.Cms.Client.Tests.csproj`
- Modify: `GrandNode.sln`
- Modify: `src/Aspire/Aspire.AppHost/Aspire.AppHost.csproj`

- [ ] **Step 1: Add RichardSzalay.MockHttp to Directory.Packages.props**

In `Directory.Packages.props`, add after `<PackageVersion Include="NUnit" ...`:
```xml
    <PackageVersion Include="RichardSzalay.MockHttp" Version="7.0.0" />
```

- [ ] **Step 2: Create Grand.Cms.Api.csproj**

Create `src/Services/Grand.Cms.Api/Grand.Cms.Api.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <Import Project="..\..\Build\Grand.Common.props" />

  <ItemGroup>
    <ProjectReference Include="..\..\Business\Grand.Business.Cms\Grand.Business.Cms.csproj" />
    <ProjectReference Include="..\..\Core\Grand.Infrastructure\Grand.Infrastructure.csproj" />
    <ProjectReference Include="..\..\Aspire\Aspire.ServiceDefaults\Aspire.ServiceDefaults.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Create Grand.Cms.Client.csproj**

Create `src/Services/Grand.Cms.Client/Grand.Cms.Client.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <Import Project="..\..\Build\Grand.Common.props" />

  <ItemGroup>
    <ProjectReference Include="..\..\Business\Grand.Business.Core\Grand.Business.Core.csproj" />
    <ProjectReference Include="..\..\Core\Grand.Infrastructure\Grand.Infrastructure.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Create Grand.Cms.Client.Tests.csproj**

Create `src/Tests/Grand.Cms.Client.Tests/Grand.Cms.Client.Tests.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <Import Project="..\..\Build\Grand.Common.props" />

  <PropertyGroup>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="Moq" />
    <PackageReference Include="MSTest.TestAdapter" />
    <PackageReference Include="MSTest.TestFramework" />
    <PackageReference Include="RichardSzalay.MockHttp" />
    <PackageReference Include="coverlet.collector">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\Services\Grand.Cms.Client\Grand.Cms.Client.csproj" />
    <ProjectReference Include="..\..\Business\Grand.Business.Core\Grand.Business.Core.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 5: Add projects to solution**

```bash
dotnet sln GrandNode.sln add src/Services/Grand.Cms.Api/Grand.Cms.Api.csproj
dotnet sln GrandNode.sln add src/Services/Grand.Cms.Client/Grand.Cms.Client.csproj
dotnet sln GrandNode.sln add src/Tests/Grand.Cms.Client.Tests/Grand.Cms.Client.Tests.csproj
```

- [ ] **Step 6: Add Grand.Cms.Api to Aspire.AppHost.csproj**

In `src/Aspire/Aspire.AppHost/Aspire.AppHost.csproj`, add inside the existing `<ItemGroup>` with project references:
```xml
    <ProjectReference Include="..\..\Services\Grand.Cms.Api\Grand.Cms.Api.csproj" />
```

- [ ] **Step 7: Verify restore builds**

```bash
dotnet restore GrandNode.sln
```
Expected: no errors (warnings about SharpCompress are ok)

---

## Task 2: Grand.Cms.Api Bootstrap

**Files:**
- Create: `src/Services/Grand.Cms.Api/Program.cs`
- Create: `src/Services/Grand.Cms.Api/ServiceAuditInfoProvider.cs`
- Create: `src/Services/Grand.Cms.Api/appsettings.json`
- Create: `src/Services/Grand.Cms.Api/wwwroot/.gitkeep`
- Create: `src/Services/Grand.Cms.Api/Properties/launchSettings.json`

- [ ] **Step 1: Create ServiceAuditInfoProvider.cs**

Create `src/Services/Grand.Cms.Api/ServiceAuditInfoProvider.cs`:
```csharp
using Grand.Data;

namespace Grand.Cms.Api;

internal sealed class ServiceAuditInfoProvider : IAuditInfoProvider
{
    public string GetCurrentUser() => "cms-service";
    public DateTime GetCurrentDateTime() => DateTime.UtcNow;
}
```

- [ ] **Step 2: Create Program.cs**

Create `src/Services/Grand.Cms.Api/Program.cs`:
```csharp
using Grand.Business.Cms.Services;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Cms.Api;
using Grand.Data;
using Grand.Data.Mongo;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.SharedKernel.Extensions;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration[SettingsConstants.ConnectionStrings];
DataSettingsManager.Initialize(
    Path.Combine(builder.Environment.ContentRootPath, CommonPath.AppData, CommonPath.SettingsFile));
if (!string.IsNullOrEmpty(connectionString))
    DataSettingsManager.Instance.LoadDataSettings(new DataSettings {
        ConnectionString = connectionString,
        DbProvider = DbProvider.MongoDB
    });

var mongoUrl = new MongoUrl(connectionString);
var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
builder.Services.AddScoped(_ => new MongoClient(clientSettings).GetDatabase(mongoUrl.DatabaseName));
builder.Services.AddScoped<IDatabaseContext, MongoDBContext>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));

builder.Services.AddSingleton<IAuditInfoProvider, ServiceAuditInfoProvider>();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton(new CacheConfig { DefaultCacheTimeMinutes = 60 });
builder.Services.AddMediatR(options =>
    options.RegisterServicesFromAssembly(typeof(BlogService).Assembly));
builder.Services.AddScoped<ICacheBase, MemoryCacheBase>();

builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddScoped<IKnowledgebaseService, KnowledgebaseService>();
builder.Services.AddScoped<IPageService, PageService>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseStaticFiles();
app.MapControllers();

await app.RunAsync();
```

- [ ] **Step 3: Create appsettings.json**

Create `src/Services/Grand.Cms.Api/appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

- [ ] **Step 4: Create wwwroot placeholder**

Create `src/Services/Grand.Cms.Api/wwwroot/.gitkeep` (empty file — prevents null WebRootPath at startup).

- [ ] **Step 5: Create launchSettings.json**

Create `src/Services/Grand.Cms.Api/Properties/launchSettings.json`:
```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5200",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

- [ ] **Step 6: Verify the API project builds**

```bash
dotnet build src/Services/Grand.Cms.Api/Grand.Cms.Api.csproj
```
Expected: Build succeeded (warnings ok)

---

## Task 3: Grand.Cms.Api — DTOs

**Files:**
- Create: `src/Services/Grand.Cms.Api/DTOs/BlogDtos.cs`
- Create: `src/Services/Grand.Cms.Api/DTOs/NewsDtos.cs`
- Create: `src/Services/Grand.Cms.Api/DTOs/KnowledgebaseDtos.cs`
- Create: `src/Services/Grand.Cms.Api/DTOs/PageDtos.cs`

- [ ] **Step 1: Create BlogDtos.cs**

Create `src/Services/Grand.Cms.Api/DTOs/BlogDtos.cs`:
```csharp
namespace Grand.Cms.Api.DTOs;

public record BlogPostDto(
    string Id,
    string Title,
    string Body,
    string BodyOverview,
    string Tags,
    DateTime? StartDateUtc,
    DateTime? EndDateUtc,
    string MetaKeywords,
    string MetaDescription,
    string MetaTitle,
    string SeName,
    string PictureId,
    bool AllowComments
);

public record BlogPostRequest(
    string Title,
    string Body,
    string BodyOverview,
    string Tags,
    DateTime? StartDateUtc,
    DateTime? EndDateUtc,
    string MetaKeywords,
    string MetaDescription,
    string MetaTitle,
    string SeName,
    string PictureId,
    bool AllowComments
);

public record PagedBlogResponse(
    IList<BlogPostDto> Items,
    int TotalCount,
    int PageIndex,
    int PageSize
);
```

- [ ] **Step 2: Create NewsDtos.cs**

Create `src/Services/Grand.Cms.Api/DTOs/NewsDtos.cs`:
```csharp
namespace Grand.Cms.Api.DTOs;

public record NewsItemDto(
    string Id,
    string Title,
    string Short,
    string Full,
    bool Published,
    DateTime? StartDateUtc,
    DateTime? EndDateUtc,
    string MetaKeywords,
    string MetaDescription,
    string MetaTitle,
    string SeName,
    string PictureId
);

public record NewsItemRequest(
    string Title,
    string Short,
    string Full,
    bool Published,
    DateTime? StartDateUtc,
    DateTime? EndDateUtc,
    string MetaKeywords,
    string MetaDescription,
    string MetaTitle,
    string SeName,
    string PictureId
);

public record PagedNewsResponse(
    IList<NewsItemDto> Items,
    int TotalCount,
    int PageIndex,
    int PageSize
);
```

- [ ] **Step 3: Create KnowledgebaseDtos.cs**

Create `src/Services/Grand.Cms.Api/DTOs/KnowledgebaseDtos.cs`:
```csharp
namespace Grand.Cms.Api.DTOs;

public record KbArticleDto(
    string Id,
    string Name,
    string Content,
    string SeName,
    string ParentCategoryId,
    bool Published,
    int DisplayOrder,
    bool ShowOnHomepage,
    string MetaKeywords,
    string MetaDescription,
    string MetaTitle
);

public record KbArticleRequest(
    string Name,
    string Content,
    string SeName,
    string ParentCategoryId,
    bool Published,
    int DisplayOrder,
    bool ShowOnHomepage,
    string MetaKeywords,
    string MetaDescription,
    string MetaTitle
);

public record KbCategoryDto(
    string Id,
    string Name,
    string Description,
    string SeName,
    string ParentCategoryId,
    bool Published,
    int DisplayOrder,
    string MetaKeywords,
    string MetaDescription,
    string MetaTitle
);

public record KbCategoryRequest(
    string Name,
    string Description,
    string SeName,
    string ParentCategoryId,
    bool Published,
    int DisplayOrder,
    string MetaKeywords,
    string MetaDescription,
    string MetaTitle
);
```

- [ ] **Step 4: Create PageDtos.cs**

Create `src/Services/Grand.Cms.Api/DTOs/PageDtos.cs`:
```csharp
namespace Grand.Cms.Api.DTOs;

public record PageDto(
    string Id,
    string SystemName,
    string Title,
    string Body,
    string SeName,
    bool Published,
    DateTime? StartDateUtc,
    DateTime? EndDateUtc,
    string MetaKeywords,
    string MetaDescription,
    string MetaTitle,
    string PageLayoutId
);

public record PageRequest(
    string SystemName,
    string Title,
    string Body,
    string SeName,
    bool Published,
    DateTime? StartDateUtc,
    DateTime? EndDateUtc,
    string MetaKeywords,
    string MetaDescription,
    string MetaTitle,
    string PageLayoutId
);
```

- [ ] **Step 5: Verify build**

```bash
dotnet build src/Services/Grand.Cms.Api/Grand.Cms.Api.csproj
```
Expected: Build succeeded

---

## Task 4 (TDD Red): Blog Tests

**Files:**
- Create: `src/Services/Grand.Cms.Client/CmsClientConstants.cs`
- Create: `src/Tests/Grand.Cms.Client.Tests/Services/CmsApiBlogServiceTests.cs`

- [ ] **Step 1: Create CmsClientConstants.cs**

Create `src/Services/Grand.Cms.Client/CmsClientConstants.cs`:
```csharp
namespace Grand.Cms.Client;

internal static class CmsClientConstants
{
    public const string HttpClientName = "cms-api";
}
```

- [ ] **Step 2: Create CmsApiBlogServiceTests.cs (3 tests, all will fail — class doesn't exist yet)**

Create `src/Tests/Grand.Cms.Client.Tests/Services/CmsApiBlogServiceTests.cs`:
```csharp
using Grand.Cms.Client.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using RichardSzalay.MockHttp;
using System.Net;

namespace Grand.Cms.Client.Tests.Services;

[TestClass]
public class CmsApiBlogServiceTests
{
    private static CmsApiBlogService CreateService(MockHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(CmsClientConstants.HttpClientName)).Returns(client);
        var cache = new MemoryCache(new MemoryCacheOptions());
        return new CmsApiBlogService(factory.Object, cache);
    }

    [TestMethod]
    public async Task GetAllBlogPosts_WhenApiReturnsItems_ReturnsMappedList()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Get, "http://localhost/api/blogs*")
               .Respond("application/json",
                   """[{"id":"1","title":"Hello","body":"World","bodyOverview":"","tags":"","startDateUtc":null,"endDateUtc":null,"metaKeywords":"","metaDescription":"","metaTitle":"","seName":"hello","pictureId":"","allowComments":false}]""");

        var svc = CreateService(handler);
        var result = await svc.GetAllBlogPosts("", null, null, 0, 10);

        Assert.AreEqual(1, result.TotalCount);
        Assert.AreEqual("Hello", result[0].Title);
    }

    [TestMethod]
    public async Task GetBlogPostById_WhenNotFound_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Get, "http://localhost/api/blogs/missing")
               .Respond(HttpStatusCode.NotFound);

        var svc = CreateService(handler);
        var result = await svc.GetBlogPostById("missing");

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task InsertBlogPost_CallsPostAndPopulatesId()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Post, "http://localhost/api/blogs")
               .Respond("application/json",
                   """{"id":"abc123","title":"New","body":"","bodyOverview":"","tags":"","startDateUtc":null,"endDateUtc":null,"metaKeywords":"","metaDescription":"","metaTitle":"","seName":"new","pictureId":"","allowComments":false}""");

        var svc = CreateService(handler);
        var post = new Grand.Domain.Blogs.BlogPost { Title = "New" };
        await svc.InsertBlogPost(post);

        Assert.AreEqual("abc123", post.Id);
    }
}
```

- [ ] **Step 3: Run tests — expect compile error (CmsApiBlogService doesn't exist)**

```bash
dotnet test src/Tests/Grand.Cms.Client.Tests/Grand.Cms.Client.Tests.csproj
```
Expected: Build error — `The type or namespace name 'CmsApiBlogService' could not be found`

---

## Task 5: Blog Controller + Client Implementation (Green)

**Files:**
- Create: `src/Services/Grand.Cms.Api/Controllers/BlogController.cs`
- Create: `src/Services/Grand.Cms.Client/DTOs/BlogClientDtos.cs`
- Create: `src/Services/Grand.Cms.Client/Services/CmsApiBlogService.cs`

- [ ] **Step 1: Create BlogController.cs**

Create `src/Services/Grand.Cms.Api/Controllers/BlogController.cs`:
```csharp
using Grand.Business.Core.Interfaces.Cms;
using Grand.Cms.Api.DTOs;
using Grand.Domain.Blogs;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Cms.Api.Controllers;

[ApiController]
[Route("api/blogs")]
public class BlogController : ControllerBase
{
    private readonly IBlogService _blog;

    public BlogController(IBlogService blog) => _blog = blog;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string storeId = "",
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool showHidden = false,
        [FromQuery] string tag = null,
        [FromQuery] string categoryId = "")
    {
        var posts = await _blog.GetAllBlogPosts(storeId, null, null, pageIndex, pageSize, showHidden, tag, "", categoryId);
        return Ok(posts.Select(ToDto).ToList());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var post = await _blog.GetBlogPostById(id);
        return post == null ? NotFound() : Ok(ToDto(post));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BlogPostRequest request)
    {
        var post = FromRequest(request);
        await _blog.InsertBlogPost(post);
        return CreatedAtAction(nameof(GetById), new { id = post.Id }, ToDto(post));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] BlogPostRequest request)
    {
        var post = await _blog.GetBlogPostById(id);
        if (post == null) return NotFound();
        ApplyRequest(post, request);
        await _blog.UpdateBlogPost(post);
        return Ok(ToDto(post));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var post = await _blog.GetBlogPostById(id);
        if (post == null) return NotFound();
        await _blog.DeleteBlogPost(post);
        return NoContent();
    }

    private static BlogPostDto ToDto(BlogPost p) => new(
        p.Id, p.Title, p.Body, p.BodyOverview, p.Tags,
        p.StartDateUtc, p.EndDateUtc, p.MetaKeywords, p.MetaDescription,
        p.MetaTitle, p.SeName, p.PictureId, p.AllowComments);

    private static BlogPost FromRequest(BlogPostRequest r) => new() {
        Title = r.Title, Body = r.Body, BodyOverview = r.BodyOverview,
        Tags = r.Tags, StartDateUtc = r.StartDateUtc, EndDateUtc = r.EndDateUtc,
        MetaKeywords = r.MetaKeywords, MetaDescription = r.MetaDescription,
        MetaTitle = r.MetaTitle, SeName = r.SeName, PictureId = r.PictureId,
        AllowComments = r.AllowComments
    };

    private static void ApplyRequest(BlogPost p, BlogPostRequest r) {
        p.Title = r.Title; p.Body = r.Body; p.BodyOverview = r.BodyOverview;
        p.Tags = r.Tags; p.StartDateUtc = r.StartDateUtc; p.EndDateUtc = r.EndDateUtc;
        p.MetaKeywords = r.MetaKeywords; p.MetaDescription = r.MetaDescription;
        p.MetaTitle = r.MetaTitle; p.SeName = r.SeName; p.PictureId = r.PictureId;
        p.AllowComments = r.AllowComments;
    }
}
```

- [ ] **Step 2: Create BlogClientDtos.cs (internal DTOs for JSON deserialization)**

Create `src/Services/Grand.Cms.Client/DTOs/BlogClientDtos.cs`:
```csharp
namespace Grand.Cms.Client.DTOs;

internal record BlogPostClientDto(
    string Id,
    string Title,
    string Body,
    string BodyOverview,
    string Tags,
    DateTime? StartDateUtc,
    DateTime? EndDateUtc,
    string MetaKeywords,
    string MetaDescription,
    string MetaTitle,
    string SeName,
    string PictureId,
    bool AllowComments
);
```

- [ ] **Step 3: Create CmsApiBlogService.cs**

Create `src/Services/Grand.Cms.Client/Services/CmsApiBlogService.cs`:
```csharp
using Grand.Business.Core.Interfaces.Cms;
using Grand.Cms.Client.DTOs;
using Grand.Domain;
using Grand.Domain.Blogs;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Json;

namespace Grand.Cms.Client.Services;

internal sealed class CmsApiBlogService : IBlogService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(60);

    public CmsApiBlogService(IHttpClientFactory factory, IMemoryCache cache)
    {
        _http = factory.CreateClient(CmsClientConstants.HttpClientName);
        _cache = cache;
    }

    public async Task<IPagedList<BlogPost>> GetAllBlogPosts(string storeId = "",
        DateTime? dateFrom = null, DateTime? dateTo = null,
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false,
        string tag = null, string blogPostName = "", string categoryId = "")
    {
        var url = $"api/blogs?storeId={storeId}&pageIndex={pageIndex}&pageSize={pageSize}&showHidden={showHidden}&tag={tag}&categoryId={categoryId}";
        var items = await _http.GetFromJsonAsync<List<BlogPostClientDto>>(url) ?? new List<BlogPostClientDto>();
        var posts = items.Select(ToEntity).ToList();
        return new PagedList<BlogPost>(posts, pageIndex, pageSize, posts.Count);
    }

    public async Task<BlogPost> GetBlogPostById(string blogPostId)
    {
        var cacheKey = $"cms:blog:{blogPostId}";
        if (_cache.TryGetValue(cacheKey, out BlogPost cached)) return cached;

        var response = await _http.GetAsync($"api/blogs/{blogPostId}");
        if (!response.IsSuccessStatusCode) return null;
        var dto = await response.Content.ReadFromJsonAsync<BlogPostClientDto>();
        if (dto == null) return null;
        var entity = ToEntity(dto);
        _cache.Set(cacheKey, entity, CacheTtl);
        return entity;
    }

    public async Task InsertBlogPost(BlogPost blogPost)
    {
        var response = await _http.PostAsJsonAsync("api/blogs", ToRequest(blogPost));
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<BlogPostClientDto>();
        blogPost.Id = dto!.Id;
    }

    public async Task UpdateBlogPost(BlogPost blogPost)
    {
        var response = await _http.PutAsJsonAsync($"api/blogs/{blogPost.Id}", ToRequest(blogPost));
        response.EnsureSuccessStatusCode();
        _cache.Remove($"cms:blog:{blogPost.Id}");
    }

    public async Task DeleteBlogPost(BlogPost blogPost)
    {
        await _http.DeleteAsync($"api/blogs/{blogPost.Id}");
        _cache.Remove($"cms:blog:{blogPost.Id}");
    }

    public Task<IPagedList<BlogPost>> GetAllBlogPostsByTag(string storeId = "", string tag = "",
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
        => GetAllBlogPosts(storeId, null, null, pageIndex, pageSize, showHidden, tag);

    public Task<IList<BlogPostTag>> GetAllBlogPostTags(string storeId, bool showHidden = false)
        => Task.FromResult<IList<BlogPostTag>>(new List<BlogPostTag>());

    public Task<IList<BlogComment>> GetAllComments(string customerId, string storeId)
        => Task.FromResult<IList<BlogComment>>(new List<BlogComment>());

    public Task<BlogComment> GetBlogCommentById(string blogCommentId)
        => Task.FromResult<BlogComment>(null);

    public Task<IList<BlogComment>> GetBlogCommentsByBlogPostId(string blogPostId)
        => Task.FromResult<IList<BlogComment>>(new List<BlogComment>());

    public Task InsertBlogComment(BlogComment blogComment) => Task.CompletedTask;
    public Task DeleteBlogComment(BlogComment blogComment) => Task.CompletedTask;

    public Task<BlogCategory> GetBlogCategoryById(string blogCategoryId)
        => Task.FromResult<BlogCategory>(null);

    public Task<IList<BlogCategory>> GetBlogCategoryByPostId(string blogPostId)
        => Task.FromResult<IList<BlogCategory>>(new List<BlogCategory>());

    public Task<BlogCategory> GetBlogCategoryBySeName(string blogCategorySeName)
        => Task.FromResult<BlogCategory>(null);

    public Task<IList<BlogCategory>> GetAllBlogCategories(string storeId = "")
        => Task.FromResult<IList<BlogCategory>>(new List<BlogCategory>());

    public Task<BlogCategory> InsertBlogCategory(BlogCategory blogCategory)
        => Task.FromResult<BlogCategory>(null);

    public Task<BlogCategory> UpdateBlogCategory(BlogCategory blogCategory)
        => Task.FromResult<BlogCategory>(null);

    public Task DeleteBlogCategory(BlogCategory blogCategory) => Task.CompletedTask;

    public Task<BlogProduct> GetBlogProductById(string id)
        => Task.FromResult<BlogProduct>(null);

    public Task InsertBlogProduct(BlogProduct blogProduct) => Task.CompletedTask;
    public Task UpdateBlogProduct(BlogProduct blogProduct) => Task.CompletedTask;
    public Task DeleteBlogProduct(BlogProduct blogProduct) => Task.CompletedTask;

    public Task<IList<BlogProduct>> GetProductsByBlogPostId(string blogPostId)
        => Task.FromResult<IList<BlogProduct>>(new List<BlogProduct>());

    private static BlogPost ToEntity(BlogPostClientDto dto) => new() {
        Id = dto.Id, Title = dto.Title, Body = dto.Body, BodyOverview = dto.BodyOverview,
        Tags = dto.Tags, StartDateUtc = dto.StartDateUtc, EndDateUtc = dto.EndDateUtc,
        MetaKeywords = dto.MetaKeywords, MetaDescription = dto.MetaDescription,
        MetaTitle = dto.MetaTitle, SeName = dto.SeName, PictureId = dto.PictureId,
        AllowComments = dto.AllowComments
    };

    private static object ToRequest(BlogPost p) => new {
        title = p.Title, body = p.Body, bodyOverview = p.BodyOverview, tags = p.Tags,
        startDateUtc = p.StartDateUtc, endDateUtc = p.EndDateUtc,
        metaKeywords = p.MetaKeywords, metaDescription = p.MetaDescription,
        metaTitle = p.MetaTitle, seName = p.SeName, pictureId = p.PictureId,
        allowComments = p.AllowComments
    };
}
```

- [ ] **Step 4: Run tests — all 3 should pass**

```bash
dotnet test src/Tests/Grand.Cms.Client.Tests/Grand.Cms.Client.Tests.csproj --filter "CmsApiBlogServiceTests"
```
Expected:
```
Passed!  - Failed: 0, Passed: 3, Skipped: 0, Total: 3
```

- [ ] **Step 5: Commit (red → green for Blog)**

```bash
git add src/Services/Grand.Cms.Api/ src/Services/Grand.Cms.Client/ src/Tests/Grand.Cms.Client.Tests/ Directory.Packages.props GrandNode.sln src/Aspire/Aspire.AppHost/Aspire.AppHost.csproj
git commit -m "test(green): blog CRUD via CMS microservice — 3 tests passing

TDD cycle complete for IBlogService:
- BlogController endpoints (GET/POST/PUT/DELETE /api/blogs)
- CmsApiBlogService HTTP client with 60-min cache
- 3 unit tests with MockHttpMessageHandler (no server required)

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 6 (TDD Red): News Tests

**Files:**
- Create: `src/Services/Grand.Cms.Client/DTOs/NewsClientDtos.cs`
- Create: `src/Tests/Grand.Cms.Client.Tests/Services/CmsApiNewsServiceTests.cs`

- [ ] **Step 1: Create NewsClientDtos.cs**

Create `src/Services/Grand.Cms.Client/DTOs/NewsClientDtos.cs`:
```csharp
namespace Grand.Cms.Client.DTOs;

internal record NewsItemClientDto(
    string Id,
    string Title,
    string Short,
    string Full,
    bool Published,
    DateTime? StartDateUtc,
    DateTime? EndDateUtc,
    string MetaKeywords,
    string MetaDescription,
    string MetaTitle,
    string SeName,
    string PictureId
);
```

- [ ] **Step 2: Create CmsApiNewsServiceTests.cs**

Create `src/Tests/Grand.Cms.Client.Tests/Services/CmsApiNewsServiceTests.cs`:
```csharp
using Grand.Cms.Client.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using RichardSzalay.MockHttp;
using System.Net;

namespace Grand.Cms.Client.Tests.Services;

[TestClass]
public class CmsApiNewsServiceTests
{
    private static CmsApiNewsService CreateService(MockHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(CmsClientConstants.HttpClientName)).Returns(client);
        var cache = new MemoryCache(new MemoryCacheOptions());
        return new CmsApiNewsService(factory.Object, cache);
    }

    [TestMethod]
    public async Task GetAllNews_WhenApiReturnsItems_ReturnsMappedList()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Get, "http://localhost/api/news*")
               .Respond("application/json",
                   """[{"id":"1","title":"Breaking","short":"Summary","full":"Full text","published":true,"startDateUtc":null,"endDateUtc":null,"metaKeywords":"","metaDescription":"","metaTitle":"","seName":"breaking","pictureId":""}]""");

        var svc = CreateService(handler);
        var result = await svc.GetAllNews("", 0, 10);

        Assert.AreEqual(1, result.TotalCount);
        Assert.AreEqual("Breaking", result[0].Title);
        Assert.IsTrue(result[0].Published);
    }

    [TestMethod]
    public async Task GetNewsById_WhenNotFound_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Get, "http://localhost/api/news/missing")
               .Respond(HttpStatusCode.NotFound);

        var svc = CreateService(handler);
        var result = await svc.GetNewsById("missing");

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task InsertNews_CallsPostAndPopulatesId()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Post, "http://localhost/api/news")
               .Respond("application/json",
                   """{"id":"news99","title":"New Article","short":"","full":"","published":false,"startDateUtc":null,"endDateUtc":null,"metaKeywords":"","metaDescription":"","metaTitle":"","seName":"","pictureId":""}""");

        var svc = CreateService(handler);
        var item = new Grand.Domain.News.NewsItem { Title = "New Article" };
        await svc.InsertNews(item);

        Assert.AreEqual("news99", item.Id);
    }
}
```

- [ ] **Step 3: Run tests — expect compile error (CmsApiNewsService doesn't exist)**

```bash
dotnet test src/Tests/Grand.Cms.Client.Tests/Grand.Cms.Client.Tests.csproj --filter "CmsApiNewsServiceTests"
```
Expected: Build error — `CmsApiNewsService could not be found`

---

## Task 7: News Controller + Client Implementation (Green)

**Files:**
- Create: `src/Services/Grand.Cms.Api/Controllers/NewsController.cs`
- Create: `src/Services/Grand.Cms.Client/Services/CmsApiNewsService.cs`

- [ ] **Step 1: Create NewsController.cs**

Create `src/Services/Grand.Cms.Api/Controllers/NewsController.cs`:
```csharp
using Grand.Business.Core.Interfaces.Cms;
using Grand.Cms.Api.DTOs;
using Grand.Domain.News;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Cms.Api.Controllers;

[ApiController]
[Route("api/news")]
public class NewsController : ControllerBase
{
    private readonly INewsService _news;

    public NewsController(INewsService news) => _news = news;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string storeId = "",
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool showHidden = false,
        [FromQuery] string newsTitle = "")
    {
        var items = await _news.GetAllNews(storeId, pageIndex, pageSize, false, showHidden, newsTitle);
        return Ok(items.Select(ToDto).ToList());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var item = await _news.GetNewsById(id);
        return item == null ? NotFound() : Ok(ToDto(item));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] NewsItemRequest request)
    {
        var item = FromRequest(request);
        await _news.InsertNews(item);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, ToDto(item));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] NewsItemRequest request)
    {
        var item = await _news.GetNewsById(id);
        if (item == null) return NotFound();
        ApplyRequest(item, request);
        await _news.UpdateNews(item);
        return Ok(ToDto(item));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var item = await _news.GetNewsById(id);
        if (item == null) return NotFound();
        await _news.DeleteNews(item);
        return NoContent();
    }

    private static NewsItemDto ToDto(NewsItem n) => new(
        n.Id, n.Title, n.Short, n.Full, n.Published,
        n.StartDateUtc, n.EndDateUtc, n.MetaKeywords, n.MetaDescription,
        n.MetaTitle, n.SeName, n.PictureId);

    private static NewsItem FromRequest(NewsItemRequest r) => new() {
        Title = r.Title, Short = r.Short, Full = r.Full, Published = r.Published,
        StartDateUtc = r.StartDateUtc, EndDateUtc = r.EndDateUtc,
        MetaKeywords = r.MetaKeywords, MetaDescription = r.MetaDescription,
        MetaTitle = r.MetaTitle, SeName = r.SeName, PictureId = r.PictureId
    };

    private static void ApplyRequest(NewsItem n, NewsItemRequest r) {
        n.Title = r.Title; n.Short = r.Short; n.Full = r.Full; n.Published = r.Published;
        n.StartDateUtc = r.StartDateUtc; n.EndDateUtc = r.EndDateUtc;
        n.MetaKeywords = r.MetaKeywords; n.MetaDescription = r.MetaDescription;
        n.MetaTitle = r.MetaTitle; n.SeName = r.SeName; n.PictureId = r.PictureId;
    }
}
```

- [ ] **Step 2: Create CmsApiNewsService.cs**

Create `src/Services/Grand.Cms.Client/Services/CmsApiNewsService.cs`:
```csharp
using Grand.Business.Core.Interfaces.Cms;
using Grand.Cms.Client.DTOs;
using Grand.Domain;
using Grand.Domain.News;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Json;

namespace Grand.Cms.Client.Services;

internal sealed class CmsApiNewsService : INewsService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(60);

    public CmsApiNewsService(IHttpClientFactory factory, IMemoryCache cache)
    {
        _http = factory.CreateClient(CmsClientConstants.HttpClientName);
        _cache = cache;
    }

    public async Task<IPagedList<NewsItem>> GetAllNews(string storeId = "",
        int pageIndex = 0, int pageSize = int.MaxValue,
        bool ignoreAcl = false, bool showHidden = false, string newsTitle = "")
    {
        var url = $"api/news?storeId={storeId}&pageIndex={pageIndex}&pageSize={pageSize}&showHidden={showHidden}&newsTitle={newsTitle}";
        var items = await _http.GetFromJsonAsync<List<NewsItemClientDto>>(url) ?? new();
        var entities = items.Select(ToEntity).ToList();
        return new PagedList<NewsItem>(entities, pageIndex, pageSize, entities.Count);
    }

    public async Task<NewsItem> GetNewsById(string newsId)
    {
        var cacheKey = $"cms:news:{newsId}";
        if (_cache.TryGetValue(cacheKey, out NewsItem cached)) return cached;

        var response = await _http.GetAsync($"api/news/{newsId}");
        if (!response.IsSuccessStatusCode) return null;
        var dto = await response.Content.ReadFromJsonAsync<NewsItemClientDto>();
        if (dto == null) return null;
        var entity = ToEntity(dto);
        _cache.Set(cacheKey, entity, CacheTtl);
        return entity;
    }

    public async Task InsertNews(NewsItem news)
    {
        var response = await _http.PostAsJsonAsync("api/news", ToRequest(news));
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<NewsItemClientDto>();
        news.Id = dto!.Id;
    }

    public async Task UpdateNews(NewsItem news)
    {
        var response = await _http.PutAsJsonAsync($"api/news/{news.Id}", ToRequest(news));
        response.EnsureSuccessStatusCode();
        _cache.Remove($"cms:news:{news.Id}");
    }

    public async Task DeleteNews(NewsItem newsItem)
    {
        await _http.DeleteAsync($"api/news/{newsItem.Id}");
        _cache.Remove($"cms:news:{newsItem.Id}");
    }

    public Task<IList<NewsComment>> GetAllComments(string customerId)
        => Task.FromResult<IList<NewsComment>>(new List<NewsComment>());

    private static NewsItem ToEntity(NewsItemClientDto dto) => new() {
        Id = dto.Id, Title = dto.Title, Short = dto.Short, Full = dto.Full,
        Published = dto.Published, StartDateUtc = dto.StartDateUtc, EndDateUtc = dto.EndDateUtc,
        MetaKeywords = dto.MetaKeywords, MetaDescription = dto.MetaDescription,
        MetaTitle = dto.MetaTitle, SeName = dto.SeName, PictureId = dto.PictureId
    };

    private static object ToRequest(NewsItem n) => new {
        title = n.Title, @short = n.Short, full = n.Full, published = n.Published,
        startDateUtc = n.StartDateUtc, endDateUtc = n.EndDateUtc,
        metaKeywords = n.MetaKeywords, metaDescription = n.MetaDescription,
        metaTitle = n.MetaTitle, seName = n.SeName, pictureId = n.PictureId
    };
}
```

- [ ] **Step 3: Run News tests — all 3 should pass**

```bash
dotnet test src/Tests/Grand.Cms.Client.Tests/Grand.Cms.Client.Tests.csproj --filter "CmsApiNewsServiceTests"
```
Expected:
```
Passed!  - Failed: 0, Passed: 3, Skipped: 0, Total: 3
```

---

## Task 8 (TDD Red): Knowledgebase Tests

**Files:**
- Create: `src/Services/Grand.Cms.Client/DTOs/KnowledgebaseClientDtos.cs`
- Create: `src/Tests/Grand.Cms.Client.Tests/Services/CmsApiKnowledgebaseServiceTests.cs`

- [ ] **Step 1: Create KnowledgebaseClientDtos.cs**

Create `src/Services/Grand.Cms.Client/DTOs/KnowledgebaseClientDtos.cs`:
```csharp
namespace Grand.Cms.Client.DTOs;

internal record KbArticleClientDto(
    string Id,
    string Name,
    string Content,
    string SeName,
    string ParentCategoryId,
    bool Published,
    int DisplayOrder,
    bool ShowOnHomepage,
    string MetaKeywords,
    string MetaDescription,
    string MetaTitle
);

internal record KbCategoryClientDto(
    string Id,
    string Name,
    string Description,
    string SeName,
    string ParentCategoryId,
    bool Published,
    int DisplayOrder,
    string MetaKeywords,
    string MetaDescription,
    string MetaTitle
);
```

- [ ] **Step 2: Create CmsApiKnowledgebaseServiceTests.cs**

Create `src/Tests/Grand.Cms.Client.Tests/Services/CmsApiKnowledgebaseServiceTests.cs`:
```csharp
using Grand.Cms.Client.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using RichardSzalay.MockHttp;
using System.Net;

namespace Grand.Cms.Client.Tests.Services;

[TestClass]
public class CmsApiKnowledgebaseServiceTests
{
    private static CmsApiKnowledgebaseService CreateService(MockHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(CmsClientConstants.HttpClientName)).Returns(client);
        var cache = new MemoryCache(new MemoryCacheOptions());
        return new CmsApiKnowledgebaseService(factory.Object, cache);
    }

    [TestMethod]
    public async Task GetKnowledgebaseArticles_WhenApiReturnsItems_ReturnsMappedList()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Get, "http://localhost/api/knowledgebase/articles*")
               .Respond("application/json",
                   """[{"id":"k1","name":"How to setup","content":"Step by step...","seName":"how-to-setup","parentCategoryId":"","published":true,"displayOrder":1,"showOnHomepage":false,"metaKeywords":"","metaDescription":"","metaTitle":""}]""");

        var svc = CreateService(handler);
        var result = await svc.GetKnowledgebaseArticles("");

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("How to setup", result[0].Name);
    }

    [TestMethod]
    public async Task GetKnowledgebaseArticle_WhenNotFound_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Get, "http://localhost/api/knowledgebase/articles/missing")
               .Respond(HttpStatusCode.NotFound);

        var svc = CreateService(handler);
        var result = await svc.GetKnowledgebaseArticle("missing");

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task InsertKnowledgebaseArticle_CallsPostAndPopulatesId()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Post, "http://localhost/api/knowledgebase/articles")
               .Respond("application/json",
                   """{"id":"ka99","name":"FAQ","content":"","seName":"faq","parentCategoryId":"","published":true,"displayOrder":0,"showOnHomepage":false,"metaKeywords":"","metaDescription":"","metaTitle":""}""");

        var svc = CreateService(handler);
        var article = new Grand.Domain.Knowledgebase.KnowledgebaseArticle { Name = "FAQ" };
        await svc.InsertKnowledgebaseArticle(article);

        Assert.AreEqual("ka99", article.Id);
    }
}
```

- [ ] **Step 3: Run — expect compile error (CmsApiKnowledgebaseService missing)**

```bash
dotnet test src/Tests/Grand.Cms.Client.Tests/Grand.Cms.Client.Tests.csproj --filter "CmsApiKnowledgebaseServiceTests"
```
Expected: Build error

---

## Task 9: Knowledgebase Controller + Client (Green)

**Files:**
- Create: `src/Services/Grand.Cms.Api/Controllers/KnowledgebaseController.cs`
- Create: `src/Services/Grand.Cms.Client/Services/CmsApiKnowledgebaseService.cs`

- [ ] **Step 1: Create KnowledgebaseController.cs**

Create `src/Services/Grand.Cms.Api/Controllers/KnowledgebaseController.cs`:
```csharp
using Grand.Business.Core.Interfaces.Cms;
using Grand.Cms.Api.DTOs;
using Grand.Domain.Knowledgebase;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Cms.Api.Controllers;

[ApiController]
[Route("api/knowledgebase")]
public class KnowledgebaseController : ControllerBase
{
    private readonly IKnowledgebaseService _kb;

    public KnowledgebaseController(IKnowledgebaseService kb) => _kb = kb;

    [HttpGet("articles")]
    public async Task<IActionResult> GetArticles([FromQuery] string storeId = "")
    {
        var articles = await _kb.GetKnowledgebaseArticles(storeId);
        return Ok(articles.Select(ToArticleDto).ToList());
    }

    [HttpGet("articles/{id}")]
    public async Task<IActionResult> GetArticleById(string id)
    {
        var article = await _kb.GetKnowledgebaseArticle(id);
        return article == null ? NotFound() : Ok(ToArticleDto(article));
    }

    [HttpPost("articles")]
    public async Task<IActionResult> CreateArticle([FromBody] KbArticleRequest request)
    {
        var article = ArticleFromRequest(request);
        await _kb.InsertKnowledgebaseArticle(article);
        return CreatedAtAction(nameof(GetArticleById), new { id = article.Id }, ToArticleDto(article));
    }

    [HttpPut("articles/{id}")]
    public async Task<IActionResult> UpdateArticle(string id, [FromBody] KbArticleRequest request)
    {
        var article = await _kb.GetKnowledgebaseArticle(id);
        if (article == null) return NotFound();
        ApplyArticleRequest(article, request);
        await _kb.UpdateKnowledgebaseArticle(article);
        return Ok(ToArticleDto(article));
    }

    [HttpDelete("articles/{id}")]
    public async Task<IActionResult> DeleteArticle(string id)
    {
        var article = await _kb.GetKnowledgebaseArticle(id);
        if (article == null) return NotFound();
        await _kb.DeleteKnowledgebaseArticle(article);
        return NoContent();
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _kb.GetKnowledgebaseCategories();
        return Ok(categories.Select(ToCategoryDto).ToList());
    }

    [HttpGet("categories/{id}")]
    public async Task<IActionResult> GetCategoryById(string id)
    {
        var category = await _kb.GetKnowledgebaseCategory(id);
        return category == null ? NotFound() : Ok(ToCategoryDto(category));
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] KbCategoryRequest request)
    {
        var category = CategoryFromRequest(request);
        await _kb.InsertKnowledgebaseCategory(category);
        return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id }, ToCategoryDto(category));
    }

    [HttpPut("categories/{id}")]
    public async Task<IActionResult> UpdateCategory(string id, [FromBody] KbCategoryRequest request)
    {
        var category = await _kb.GetKnowledgebaseCategory(id);
        if (category == null) return NotFound();
        ApplyCategoryRequest(category, request);
        await _kb.UpdateKnowledgebaseCategory(category);
        return Ok(ToCategoryDto(category));
    }

    [HttpDelete("categories/{id}")]
    public async Task<IActionResult> DeleteCategory(string id)
    {
        var category = await _kb.GetKnowledgebaseCategory(id);
        if (category == null) return NotFound();
        await _kb.DeleteKnowledgebaseCategory(category);
        return NoContent();
    }

    private static KbArticleDto ToArticleDto(KnowledgebaseArticle a) => new(
        a.Id, a.Name, a.Content, a.SeName, a.ParentCategoryId, a.Published,
        a.DisplayOrder, a.ShowOnHomepage, a.MetaKeywords, a.MetaDescription, a.MetaTitle);

    private static KnowledgebaseArticle ArticleFromRequest(KbArticleRequest r) => new() {
        Name = r.Name, Content = r.Content, SeName = r.SeName, ParentCategoryId = r.ParentCategoryId,
        Published = r.Published, DisplayOrder = r.DisplayOrder, ShowOnHomepage = r.ShowOnHomepage,
        MetaKeywords = r.MetaKeywords, MetaDescription = r.MetaDescription, MetaTitle = r.MetaTitle
    };

    private static void ApplyArticleRequest(KnowledgebaseArticle a, KbArticleRequest r) {
        a.Name = r.Name; a.Content = r.Content; a.SeName = r.SeName; a.ParentCategoryId = r.ParentCategoryId;
        a.Published = r.Published; a.DisplayOrder = r.DisplayOrder; a.ShowOnHomepage = r.ShowOnHomepage;
        a.MetaKeywords = r.MetaKeywords; a.MetaDescription = r.MetaDescription; a.MetaTitle = r.MetaTitle;
    }

    private static KbCategoryDto ToCategoryDto(KnowledgebaseCategory c) => new(
        c.Id, c.Name, c.Description, c.SeName, c.ParentCategoryId, c.Published,
        c.DisplayOrder, c.MetaKeywords, c.MetaDescription, c.MetaTitle);

    private static KnowledgebaseCategory CategoryFromRequest(KbCategoryRequest r) => new() {
        Name = r.Name, Description = r.Description, SeName = r.SeName, ParentCategoryId = r.ParentCategoryId,
        Published = r.Published, DisplayOrder = r.DisplayOrder,
        MetaKeywords = r.MetaKeywords, MetaDescription = r.MetaDescription, MetaTitle = r.MetaTitle
    };

    private static void ApplyCategoryRequest(KnowledgebaseCategory c, KbCategoryRequest r) {
        c.Name = r.Name; c.Description = r.Description; c.SeName = r.SeName; c.ParentCategoryId = r.ParentCategoryId;
        c.Published = r.Published; c.DisplayOrder = r.DisplayOrder;
        c.MetaKeywords = r.MetaKeywords; c.MetaDescription = r.MetaDescription; c.MetaTitle = r.MetaTitle;
    }
}
```

- [ ] **Step 2: Create CmsApiKnowledgebaseService.cs**

Create `src/Services/Grand.Cms.Client/Services/CmsApiKnowledgebaseService.cs`:
```csharp
using Grand.Business.Core.Interfaces.Cms;
using Grand.Cms.Client.DTOs;
using Grand.Domain;
using Grand.Domain.Knowledgebase;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Json;

namespace Grand.Cms.Client.Services;

internal sealed class CmsApiKnowledgebaseService : IKnowledgebaseService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(60);

    public CmsApiKnowledgebaseService(IHttpClientFactory factory, IMemoryCache cache)
    {
        _http = factory.CreateClient(CmsClientConstants.HttpClientName);
        _cache = cache;
    }

    public async Task<List<KnowledgebaseArticle>> GetKnowledgebaseArticles(string storeId = "")
    {
        var items = await _http.GetFromJsonAsync<List<KbArticleClientDto>>($"api/knowledgebase/articles?storeId={storeId}") ?? new();
        return items.Select(ToArticleEntity).ToList();
    }

    public async Task<KnowledgebaseArticle> GetKnowledgebaseArticle(string id)
    {
        var cacheKey = $"cms:kba:{id}";
        if (_cache.TryGetValue(cacheKey, out KnowledgebaseArticle cached)) return cached;

        var response = await _http.GetAsync($"api/knowledgebase/articles/{id}");
        if (!response.IsSuccessStatusCode) return null;
        var dto = await response.Content.ReadFromJsonAsync<KbArticleClientDto>();
        if (dto == null) return null;
        var entity = ToArticleEntity(dto);
        _cache.Set(cacheKey, entity, CacheTtl);
        return entity;
    }

    public async Task InsertKnowledgebaseArticle(KnowledgebaseArticle ka)
    {
        var response = await _http.PostAsJsonAsync("api/knowledgebase/articles", ToArticleRequest(ka));
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<KbArticleClientDto>();
        ka.Id = dto!.Id;
    }

    public async Task UpdateKnowledgebaseArticle(KnowledgebaseArticle ka)
    {
        var response = await _http.PutAsJsonAsync($"api/knowledgebase/articles/{ka.Id}", ToArticleRequest(ka));
        response.EnsureSuccessStatusCode();
        _cache.Remove($"cms:kba:{ka.Id}");
    }

    public async Task DeleteKnowledgebaseArticle(KnowledgebaseArticle ka)
    {
        await _http.DeleteAsync($"api/knowledgebase/articles/{ka.Id}");
        _cache.Remove($"cms:kba:{ka.Id}");
    }

    public async Task<List<KnowledgebaseCategory>> GetKnowledgebaseCategories()
    {
        var items = await _http.GetFromJsonAsync<List<KbCategoryClientDto>>("api/knowledgebase/categories") ?? new();
        return items.Select(ToCategoryEntity).ToList();
    }

    public async Task<KnowledgebaseCategory> GetKnowledgebaseCategory(string id)
    {
        var cacheKey = $"cms:kbc:{id}";
        if (_cache.TryGetValue(cacheKey, out KnowledgebaseCategory cached)) return cached;

        var response = await _http.GetAsync($"api/knowledgebase/categories/{id}");
        if (!response.IsSuccessStatusCode) return null;
        var dto = await response.Content.ReadFromJsonAsync<KbCategoryClientDto>();
        if (dto == null) return null;
        var entity = ToCategoryEntity(dto);
        _cache.Set(cacheKey, entity, CacheTtl);
        return entity;
    }

    public async Task InsertKnowledgebaseCategory(KnowledgebaseCategory kc)
    {
        var response = await _http.PostAsJsonAsync("api/knowledgebase/categories", ToCategoryRequest(kc));
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<KbCategoryClientDto>();
        kc.Id = dto!.Id;
    }

    public async Task UpdateKnowledgebaseCategory(KnowledgebaseCategory kc)
    {
        var response = await _http.PutAsJsonAsync($"api/knowledgebase/categories/{kc.Id}", ToCategoryRequest(kc));
        response.EnsureSuccessStatusCode();
        _cache.Remove($"cms:kbc:{kc.Id}");
    }

    public async Task DeleteKnowledgebaseCategory(KnowledgebaseCategory kc)
    {
        await _http.DeleteAsync($"api/knowledgebase/categories/{kc.Id}");
        _cache.Remove($"cms:kbc:{kc.Id}");
    }

    public Task<KnowledgebaseCategory> GetPublicKnowledgebaseCategory(string id)
        => GetKnowledgebaseCategory(id);

    public Task<List<KnowledgebaseCategory>> GetPublicKnowledgebaseCategories()
        => GetKnowledgebaseCategories();

    public Task<List<KnowledgebaseArticle>> GetPublicKnowledgebaseArticles()
        => GetKnowledgebaseArticles();

    public Task<List<KnowledgebaseArticle>> GetHomepageKnowledgebaseArticles()
        => Task.FromResult(new List<KnowledgebaseArticle>());

    public Task<List<KnowledgebaseArticle>> GetPublicKnowledgebaseArticlesByCategory(string categoryId)
        => Task.FromResult(new List<KnowledgebaseArticle>());

    public Task<List<KnowledgebaseArticle>> GetPublicKnowledgebaseArticlesByKeyword(string keyword)
        => Task.FromResult(new List<KnowledgebaseArticle>());

    public Task<List<KnowledgebaseCategory>> GetPublicKnowledgebaseCategoriesByKeyword(string keyword)
        => Task.FromResult(new List<KnowledgebaseCategory>());

    public Task<KnowledgebaseArticle> GetPublicKnowledgebaseArticle(string id)
        => GetKnowledgebaseArticle(id);

    public Task<IPagedList<KnowledgebaseArticle>> GetKnowledgebaseArticlesByCategoryId(string id,
        int pageIndex = 0, int pageSize = int.MaxValue)
        => Task.FromResult<IPagedList<KnowledgebaseArticle>>(
            new PagedList<KnowledgebaseArticle>(new List<KnowledgebaseArticle>(), pageIndex, pageSize, 0));

    public Task<IPagedList<KnowledgebaseArticle>> GetKnowledgebaseArticlesByName(string name,
        int pageIndex = 0, int pageSize = int.MaxValue)
        => Task.FromResult<IPagedList<KnowledgebaseArticle>>(
            new PagedList<KnowledgebaseArticle>(new List<KnowledgebaseArticle>(), pageIndex, pageSize, 0));

    public Task<IPagedList<KnowledgebaseArticle>> GetRelatedKnowledgebaseArticles(string articleId,
        int pageIndex = 0, int pageSize = int.MaxValue)
        => Task.FromResult<IPagedList<KnowledgebaseArticle>>(
            new PagedList<KnowledgebaseArticle>(new List<KnowledgebaseArticle>(), pageIndex, pageSize, 0));

    public Task InsertArticleComment(KnowledgebaseArticleComment articleComment) => Task.CompletedTask;

    public Task<IList<KnowledgebaseArticleComment>> GetArticleCommentsByArticleId(string articleId)
        => Task.FromResult<IList<KnowledgebaseArticleComment>>(new List<KnowledgebaseArticleComment>());

    public Task DeleteArticleComment(KnowledgebaseArticleComment articleComment) => Task.CompletedTask;

    private static KnowledgebaseArticle ToArticleEntity(KbArticleClientDto dto) => new() {
        Id = dto.Id, Name = dto.Name, Content = dto.Content, SeName = dto.SeName,
        ParentCategoryId = dto.ParentCategoryId, Published = dto.Published,
        DisplayOrder = dto.DisplayOrder, ShowOnHomepage = dto.ShowOnHomepage,
        MetaKeywords = dto.MetaKeywords, MetaDescription = dto.MetaDescription, MetaTitle = dto.MetaTitle
    };

    private static KnowledgebaseCategory ToCategoryEntity(KbCategoryClientDto dto) => new() {
        Id = dto.Id, Name = dto.Name, Description = dto.Description, SeName = dto.SeName,
        ParentCategoryId = dto.ParentCategoryId, Published = dto.Published,
        DisplayOrder = dto.DisplayOrder,
        MetaKeywords = dto.MetaKeywords, MetaDescription = dto.MetaDescription, MetaTitle = dto.MetaTitle
    };

    private static object ToArticleRequest(KnowledgebaseArticle a) => new {
        name = a.Name, content = a.Content, seName = a.SeName, parentCategoryId = a.ParentCategoryId,
        published = a.Published, displayOrder = a.DisplayOrder, showOnHomepage = a.ShowOnHomepage,
        metaKeywords = a.MetaKeywords, metaDescription = a.MetaDescription, metaTitle = a.MetaTitle
    };

    private static object ToCategoryRequest(KnowledgebaseCategory c) => new {
        name = c.Name, description = c.Description, seName = c.SeName, parentCategoryId = c.ParentCategoryId,
        published = c.Published, displayOrder = c.DisplayOrder,
        metaKeywords = c.MetaKeywords, metaDescription = c.MetaDescription, metaTitle = c.MetaTitle
    };
}
```

- [ ] **Step 3: Run KB tests — all 3 should pass**

```bash
dotnet test src/Tests/Grand.Cms.Client.Tests/Grand.Cms.Client.Tests.csproj --filter "CmsApiKnowledgebaseServiceTests"
```
Expected: `Passed!  - Failed: 0, Passed: 3, Skipped: 0, Total: 3`

---

## Task 10 (TDD Red): Pages Tests

**Files:**
- Create: `src/Services/Grand.Cms.Client/DTOs/PageClientDtos.cs`
- Create: `src/Tests/Grand.Cms.Client.Tests/Services/CmsApiPageServiceTests.cs`

- [ ] **Step 1: Create PageClientDtos.cs**

Create `src/Services/Grand.Cms.Client/DTOs/PageClientDtos.cs`:
```csharp
namespace Grand.Cms.Client.DTOs;

internal record PageClientDto(
    string Id,
    string SystemName,
    string Title,
    string Body,
    string SeName,
    bool Published,
    DateTime? StartDateUtc,
    DateTime? EndDateUtc,
    string MetaKeywords,
    string MetaDescription,
    string MetaTitle,
    string PageLayoutId
);
```

- [ ] **Step 2: Create CmsApiPageServiceTests.cs**

Create `src/Tests/Grand.Cms.Client.Tests/Services/CmsApiPageServiceTests.cs`:
```csharp
using Grand.Cms.Client.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using RichardSzalay.MockHttp;
using System.Net;

namespace Grand.Cms.Client.Tests.Services;

[TestClass]
public class CmsApiPageServiceTests
{
    private static CmsApiPageService CreateService(MockHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(CmsClientConstants.HttpClientName)).Returns(client);
        var cache = new MemoryCache(new MemoryCacheOptions());
        return new CmsApiPageService(factory.Object, cache);
    }

    [TestMethod]
    public async Task GetAllPages_WhenApiReturnsItems_ReturnsMappedList()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Get, "http://localhost/api/pages*")
               .Respond("application/json",
                   """[{"id":"p1","systemName":"about-us","title":"About Us","body":"<p>About</p>","seName":"about-us","published":true,"startDateUtc":null,"endDateUtc":null,"metaKeywords":"","metaDescription":"","metaTitle":"","pageLayoutId":""}]""");

        var svc = CreateService(handler);
        var result = await svc.GetAllPages("");

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("about-us", result[0].SystemName);
    }

    [TestMethod]
    public async Task GetPageById_WhenNotFound_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Get, "http://localhost/api/pages/missing")
               .Respond(HttpStatusCode.NotFound);

        var svc = CreateService(handler);
        var result = await svc.GetPageById("missing");

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task InsertPage_CallsPostAndPopulatesId()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Post, "http://localhost/api/pages")
               .Respond("application/json",
                   """{"id":"pg99","systemName":"new-page","title":"New Page","body":"","seName":"new-page","published":false,"startDateUtc":null,"endDateUtc":null,"metaKeywords":"","metaDescription":"","metaTitle":"","pageLayoutId":""}""");

        var svc = CreateService(handler);
        var page = new Grand.Domain.Pages.Page { SystemName = "new-page", Title = "New Page" };
        await svc.InsertPage(page);

        Assert.AreEqual("pg99", page.Id);
    }
}
```

- [ ] **Step 3: Run — expect compile error (CmsApiPageService missing)**

```bash
dotnet test src/Tests/Grand.Cms.Client.Tests/Grand.Cms.Client.Tests.csproj --filter "CmsApiPageServiceTests"
```
Expected: Build error

---

## Task 11: Pages Controller + Client (Green)

**Files:**
- Create: `src/Services/Grand.Cms.Api/Controllers/PagesController.cs`
- Create: `src/Services/Grand.Cms.Client/Services/CmsApiPageService.cs`

- [ ] **Step 1: Create PagesController.cs**

Create `src/Services/Grand.Cms.Api/Controllers/PagesController.cs`:
```csharp
using Grand.Business.Core.Interfaces.Cms;
using Grand.Cms.Api.DTOs;
using Grand.Domain.Pages;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Cms.Api.Controllers;

[ApiController]
[Route("api/pages")]
public class PagesController : ControllerBase
{
    private readonly IPageService _pages;

    public PagesController(IPageService pages) => _pages = pages;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string storeId = "")
    {
        var pages = await _pages.GetAllPages(storeId);
        return Ok(pages.Select(ToDto).ToList());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var page = await _pages.GetPageById(id);
        return page == null ? NotFound() : Ok(ToDto(page));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PageRequest request)
    {
        var page = FromRequest(request);
        await _pages.InsertPage(page);
        return CreatedAtAction(nameof(GetById), new { id = page.Id }, ToDto(page));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] PageRequest request)
    {
        var page = await _pages.GetPageById(id);
        if (page == null) return NotFound();
        ApplyRequest(page, request);
        await _pages.UpdatePage(page);
        return Ok(ToDto(page));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var page = await _pages.GetPageById(id);
        if (page == null) return NotFound();
        await _pages.DeletePage(page);
        return NoContent();
    }

    private static PageDto ToDto(Page p) => new(
        p.Id, p.SystemName, p.Title, p.Body, p.SeName, p.Published,
        p.StartDateUtc, p.EndDateUtc, p.MetaKeywords, p.MetaDescription,
        p.MetaTitle, p.PageLayoutId);

    private static Page FromRequest(PageRequest r) => new() {
        SystemName = r.SystemName, Title = r.Title, Body = r.Body, SeName = r.SeName,
        Published = r.Published, StartDateUtc = r.StartDateUtc, EndDateUtc = r.EndDateUtc,
        MetaKeywords = r.MetaKeywords, MetaDescription = r.MetaDescription,
        MetaTitle = r.MetaTitle, PageLayoutId = r.PageLayoutId
    };

    private static void ApplyRequest(Page p, PageRequest r) {
        p.SystemName = r.SystemName; p.Title = r.Title; p.Body = r.Body; p.SeName = r.SeName;
        p.Published = r.Published; p.StartDateUtc = r.StartDateUtc; p.EndDateUtc = r.EndDateUtc;
        p.MetaKeywords = r.MetaKeywords; p.MetaDescription = r.MetaDescription;
        p.MetaTitle = r.MetaTitle; p.PageLayoutId = r.PageLayoutId;
    }
}
```

- [ ] **Step 2: Create CmsApiPageService.cs**

Create `src/Services/Grand.Cms.Client/Services/CmsApiPageService.cs`:
```csharp
using Grand.Business.Core.Interfaces.Cms;
using Grand.Cms.Client.DTOs;
using Grand.Domain.Pages;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Json;

namespace Grand.Cms.Client.Services;

internal sealed class CmsApiPageService : IPageService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(60);

    public CmsApiPageService(IHttpClientFactory factory, IMemoryCache cache)
    {
        _http = factory.CreateClient(CmsClientConstants.HttpClientName);
        _cache = cache;
    }

    public async Task<IList<Page>> GetAllPages(string storeId, bool ignoreAcl = false)
    {
        var items = await _http.GetFromJsonAsync<List<PageClientDto>>($"api/pages?storeId={storeId}") ?? new();
        return items.Select(ToEntity).ToList();
    }

    public async Task<Page> GetPageById(string pageId)
    {
        var cacheKey = $"cms:page:{pageId}";
        if (_cache.TryGetValue(cacheKey, out Page cached)) return cached;

        var response = await _http.GetAsync($"api/pages/{pageId}");
        if (!response.IsSuccessStatusCode) return null;
        var dto = await response.Content.ReadFromJsonAsync<PageClientDto>();
        if (dto == null) return null;
        var entity = ToEntity(dto);
        _cache.Set(cacheKey, entity, CacheTtl);
        return entity;
    }

    public async Task<Page> GetPageBySystemName(string systemName, string storeId = "")
    {
        var pages = await GetAllPages(storeId);
        return pages.FirstOrDefault(p => p.SystemName == systemName);
    }

    public async Task InsertPage(Page page)
    {
        var response = await _http.PostAsJsonAsync("api/pages", ToRequest(page));
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<PageClientDto>();
        page.Id = dto!.Id;
    }

    public async Task UpdatePage(Page page)
    {
        var response = await _http.PutAsJsonAsync($"api/pages/{page.Id}", ToRequest(page));
        response.EnsureSuccessStatusCode();
        _cache.Remove($"cms:page:{page.Id}");
    }

    public async Task DeletePage(Page page)
    {
        await _http.DeleteAsync($"api/pages/{page.Id}");
        _cache.Remove($"cms:page:{page.Id}");
    }

    private static Page ToEntity(PageClientDto dto) => new() {
        Id = dto.Id, SystemName = dto.SystemName, Title = dto.Title, Body = dto.Body,
        SeName = dto.SeName, Published = dto.Published, StartDateUtc = dto.StartDateUtc,
        EndDateUtc = dto.EndDateUtc, MetaKeywords = dto.MetaKeywords,
        MetaDescription = dto.MetaDescription, MetaTitle = dto.MetaTitle, PageLayoutId = dto.PageLayoutId
    };

    private static object ToRequest(Page p) => new {
        systemName = p.SystemName, title = p.Title, body = p.Body, seName = p.SeName,
        published = p.Published, startDateUtc = p.StartDateUtc, endDateUtc = p.EndDateUtc,
        metaKeywords = p.MetaKeywords, metaDescription = p.MetaDescription,
        metaTitle = p.MetaTitle, pageLayoutId = p.PageLayoutId
    };
}
```

- [ ] **Step 3: Run all tests — all 12 should pass**

```bash
dotnet test src/Tests/Grand.Cms.Client.Tests/Grand.Cms.Client.Tests.csproj
```
Expected:
```
Passed!  - Failed: 0, Passed: 12, Skipped: 0, Total: 12
```

- [ ] **Step 4: Commit TDD cycle complete**

```bash
git add src/Services/Grand.Cms.Api/Controllers/ src/Services/Grand.Cms.Client/
git add src/Tests/Grand.Cms.Client.Tests/
git commit -m "test(green): all 12 CMS client tests passing — News, KB, Pages

TDD cycles complete for INewsService, IKnowledgebaseService, IPageService.
All services use MockHttpMessageHandler unit tests (no server required).

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 12: CmsClientStartup

**Files:**
- Create: `src/Services/Grand.Cms.Client/Startup/CmsClientStartup.cs`

- [ ] **Step 1: Create CmsClientStartup.cs**

Create `src/Services/Grand.Cms.Client/Startup/CmsClientStartup.cs`:
```csharp
using Grand.Business.Core.Interfaces.Cms;
using Grand.Cms.Client.Services;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Cms.Client.Startup;

public class CmsClientStartup : IStartupApplication
{
    public int Priority => 100;
    public bool BeforeConfigure => false;

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var baseAddress =
            configuration["services__grand-cms-api__http__0"]
            ?? configuration["services__grand-cms-api__https__0"]
            ?? configuration["CmsApi:BaseAddress"]
            ?? "http://localhost:5200";

        services.AddHttpClient(CmsClientConstants.HttpClientName, client => {
            client.BaseAddress = new Uri(baseAddress);
        });

        services.AddMemoryCache();
        services.AddScoped<IBlogService, CmsApiBlogService>();
        services.AddScoped<INewsService, CmsApiNewsService>();
        services.AddScoped<IKnowledgebaseService, CmsApiKnowledgebaseService>();
        services.AddScoped<IPageService, CmsApiPageService>();
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment) { }
}
```

- [ ] **Step 2: Verify Grand.Cms.Client builds**

```bash
dotnet build src/Services/Grand.Cms.Client/Grand.Cms.Client.csproj
```
Expected: Build succeeded

---

## Task 13: Aspire Wiring + Grand.Web.Common Swap

**Files:**
- Modify: `src/Aspire/Aspire.AppHost/ProjectConfiguration.cs`
- Modify: `src/Aspire/Aspire.AppHost/Program.cs`
- Modify: `src/Web/Grand.Web.Common/Grand.Web.Common.csproj`

- [ ] **Step 1: Add ConfigureGrandCmsApiProject to ProjectConfiguration.cs**

In `src/Aspire/Aspire.AppHost/ProjectConfiguration.cs`, add after `ConfigureGrandStorageApiProject`:
```csharp
    public static IResourceBuilder<ProjectResource> ConfigureGrandCmsApiProject(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> mongodb)
    {
        return builder
            .AddProject<Projects.Grand_Cms_Api>("grand-cms-api")
            .WithHttpEndpoint(5200, name: "cms")
            .WithReference(mongodb);
    }
```

Also update `ConfigureGrandWebProject` signature to accept optional `cmsApi`:
```csharp
    public static IResourceBuilder<ProjectResource> ConfigureGrandWebProject(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> mongodb,
        IResourceBuilder<ProjectResource> storageApi = null,
        IResourceBuilder<ProjectResource> cmsApi = null)
    {
        var project = builder
            .AddProject<Projects.Grand_Web>("grand-web")
            .WithHttpEndpoint(80, name: "front")
            .WithReference(mongodb);

        if (storageApi != null)
            project = project.WithReference(storageApi).WaitFor(storageApi);

        if (cmsApi != null)
            project = project.WithReference(cmsApi).WaitFor(cmsApi);

        return project;
    }
```

- [ ] **Step 2: Update Aspire Program.cs to wire cmsApi**

Replace the content of `src/Aspire/Aspire.AppHost/Program.cs`:
```csharp
using Aspire.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var mongodb = builder.AddConnectionString("Mongodb");

var storageApi = builder.ConfigureGrandStorageApiProject(mongodb);
var cmsApi = builder.ConfigureGrandCmsApiProject(mongodb);
var grandWeb = builder.ConfigureGrandWebProject(mongodb, storageApi, cmsApi);
builder.ConfigureGrandGatewayProject(grandWeb);

await builder.Build().RunAsync();
```

- [ ] **Step 3: Swap Grand.Business.Cms → Grand.Cms.Client in Grand.Web.Common.csproj**

In `src/Web/Grand.Web.Common/Grand.Web.Common.csproj`, find the line:
```xml
    <ProjectReference Include="..\..\Business\Grand.Business.Cms\Grand.Business.Cms.csproj" />
```
Replace it with:
```xml
    <ProjectReference Include="..\..\Services\Grand.Cms.Client\Grand.Cms.Client.csproj" />
```

- [ ] **Step 4: Verify full solution builds**

```bash
dotnet build GrandNode.sln
```
Expected: Build succeeded (warnings ok). If there are errors referencing `Grand.Business.Cms` types directly in `Grand.Web.Common`, they will show here — fix by ensuring all references use the interface types from `Grand.Business.Core`.

---

## Task 14: Verify, Commit, and PR

- [ ] **Step 1: Run full test suite**

```bash
dotnet test src/Tests/Grand.Cms.Client.Tests/Grand.Cms.Client.Tests.csproj
```
Expected: `Passed!  - Failed: 0, Passed: 12, Skipped: 0, Total: 12`

- [ ] **Step 2: Confirm Grand.Business.Cms no longer referenced by Web.Common**

```bash
grep -r "Grand.Business.Cms" src/Web/Grand.Web.Common/
```
Expected: no output (zero matches)

- [ ] **Step 3: Commit final wiring**

```bash
git add src/Aspire/Aspire.AppHost/Program.cs src/Aspire/Aspire.AppHost/ProjectConfiguration.cs
git add src/Services/Grand.Cms.Client/Startup/
git add src/Web/Grand.Web.Common/Grand.Web.Common.csproj
git commit -m "feat: wire Grand.Cms.Api + Grand.Cms.Client into Aspire and Grand.Web.Common

- CmsClientStartup registers all 4 CMS services via HTTP
- Grand.Cms.Api wired in Aspire on port 5200
- Grand.Web.Common no longer references Grand.Business.Cms

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

- [ ] **Step 4: Create PR**

```bash
git push -u origin feature/cms-microservice
gh pr create \
  --title "feat: extract Grand.Business.Cms as CMS microservice" \
  --body "$(cat <<'EOF'
## Summary
- Adds `Grand.Cms.Api` (port 5200): standalone ASP.NET Core Web API exposing CRUD endpoints for Blog, News, Knowledgebase, and Pages backed by Grand.Business.Cms + MongoDB
- Adds `Grand.Cms.Client`: HTTP wrapper implementing IBlogService, INewsService, IKnowledgebaseService, IPageService with 60-min caching
- Removes `Grand.Business.Cms` from `Grand.Web.Common.csproj` — replaces with `Grand.Cms.Client`
- Adds `Grand.Cms.Client.Tests` with 12 unit tests using MockHttpMessageHandler (no running server required)
- Wires `grand-cms-api` into Aspire orchestration alongside the existing Storage API

## Test plan
- [ ] `dotnet test src/Tests/Grand.Cms.Client.Tests/` — 12 tests green
- [ ] `dotnet build GrandNode.sln` — clean build
- [ ] `dotnet run --project src/Aspire/Aspire.AppHost/` — grand-cms-api appears in Aspire dashboard
- [ ] `curl http://localhost:5200/api/blogs` — returns `[]` (empty list)
- [ ] `grep -r "Grand.Business.Cms" src/Web/Grand.Web.Common/` — no matches

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
  --base develop
```

- [ ] **Step 5: Verify PR URL printed by gh and share it**

Expected: `https://github.com/<owner>/grandnode2/pull/<N>`
