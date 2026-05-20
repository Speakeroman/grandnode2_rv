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

    // ── Articles ────────────────────────────────────────────────────────────

    public async Task<List<KnowledgebaseArticle>> GetKnowledgebaseArticles(string storeId = "")
    {
        var items = await _http.GetFromJsonAsync<List<KbArticleClientDto>>(
            $"api/knowledgebase/articles?storeId={storeId}") ?? new List<KbArticleClientDto>();
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
        if (dto == null) throw new InvalidOperationException("API returned null response for InsertKnowledgebaseArticle");
        ka.Id = dto.Id;
    }

    public async Task UpdateKnowledgebaseArticle(KnowledgebaseArticle ka)
    {
        var response = await _http.PutAsJsonAsync($"api/knowledgebase/articles/{ka.Id}", ToArticleRequest(ka));
        response.EnsureSuccessStatusCode();
        _cache.Remove($"cms:kba:{ka.Id}");
    }

    public async Task DeleteKnowledgebaseArticle(KnowledgebaseArticle ka)
    {
        var response = await _http.DeleteAsync($"api/knowledgebase/articles/{ka.Id}");
        response.EnsureSuccessStatusCode();
        _cache.Remove($"cms:kba:{ka.Id}");
    }

    // ── Categories ──────────────────────────────────────────────────────────

    public async Task<List<KnowledgebaseCategory>> GetKnowledgebaseCategories()
    {
        var items = await _http.GetFromJsonAsync<List<KbCategoryClientDto>>(
            "api/knowledgebase/categories") ?? new List<KbCategoryClientDto>();
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
        if (dto == null) throw new InvalidOperationException("API returned null response for InsertKnowledgebaseCategory");
        kc.Id = dto.Id;
    }

    public async Task UpdateKnowledgebaseCategory(KnowledgebaseCategory kc)
    {
        var response = await _http.PutAsJsonAsync($"api/knowledgebase/categories/{kc.Id}", ToCategoryRequest(kc));
        response.EnsureSuccessStatusCode();
        _cache.Remove($"cms:kbc:{kc.Id}");
    }

    public async Task DeleteKnowledgebaseCategory(KnowledgebaseCategory kc)
    {
        var response = await _http.DeleteAsync($"api/knowledgebase/categories/{kc.Id}");
        response.EnsureSuccessStatusCode();
        _cache.Remove($"cms:kbc:{kc.Id}");
    }

    // ── Stub / public-facing delegates ──────────────────────────────────────

    public Task<KnowledgebaseCategory> GetPublicKnowledgebaseCategory(string id)
        => GetKnowledgebaseCategory(id);

    public Task<List<KnowledgebaseCategory>> GetPublicKnowledgebaseCategories()
        => GetKnowledgebaseCategories();

    public Task<List<KnowledgebaseArticle>> GetPublicKnowledgebaseArticles()
        => GetKnowledgebaseArticles();

    public Task<KnowledgebaseArticle> GetPublicKnowledgebaseArticle(string id)
        => GetKnowledgebaseArticle(id);

    public Task<List<KnowledgebaseArticle>> GetHomepageKnowledgebaseArticles()
        => Task.FromResult(new List<KnowledgebaseArticle>());

    public Task<List<KnowledgebaseArticle>> GetPublicKnowledgebaseArticlesByCategory(string categoryId)
        => Task.FromResult(new List<KnowledgebaseArticle>());

    public Task<List<KnowledgebaseArticle>> GetPublicKnowledgebaseArticlesByKeyword(string keyword)
        => Task.FromResult(new List<KnowledgebaseArticle>());

    public Task<List<KnowledgebaseCategory>> GetPublicKnowledgebaseCategoriesByKeyword(string keyword)
        => Task.FromResult(new List<KnowledgebaseCategory>());

    public Task<IPagedList<KnowledgebaseArticle>> GetKnowledgebaseArticlesByCategoryId(
        string id, int pageIndex = 0, int pageSize = int.MaxValue)
        => Task.FromResult<IPagedList<KnowledgebaseArticle>>(
            new PagedList<KnowledgebaseArticle>(new List<KnowledgebaseArticle>(), pageIndex, pageSize, 0));

    public Task<IPagedList<KnowledgebaseArticle>> GetKnowledgebaseArticlesByName(
        string name, int pageIndex = 0, int pageSize = int.MaxValue)
        => Task.FromResult<IPagedList<KnowledgebaseArticle>>(
            new PagedList<KnowledgebaseArticle>(new List<KnowledgebaseArticle>(), pageIndex, pageSize, 0));

    public Task<IPagedList<KnowledgebaseArticle>> GetRelatedKnowledgebaseArticles(
        string articleId, int pageIndex = 0, int pageSize = int.MaxValue)
        => Task.FromResult<IPagedList<KnowledgebaseArticle>>(
            new PagedList<KnowledgebaseArticle>(new List<KnowledgebaseArticle>(), pageIndex, pageSize, 0));

    public Task InsertArticleComment(KnowledgebaseArticleComment articleComment)
        => Task.CompletedTask;

    public Task<IList<KnowledgebaseArticleComment>> GetArticleCommentsByArticleId(string articleId)
        => Task.FromResult<IList<KnowledgebaseArticleComment>>(new List<KnowledgebaseArticleComment>());

    public Task DeleteArticleComment(KnowledgebaseArticleComment articleComment)
        => Task.CompletedTask;

    // ── Mapping helpers ─────────────────────────────────────────────────────

    private static KnowledgebaseArticle ToArticleEntity(KbArticleClientDto dto) => new() {
        Id = dto.Id, Name = dto.Name, Content = dto.Content, SeName = dto.SeName,
        ParentCategoryId = dto.ParentCategoryId, Published = dto.Published,
        DisplayOrder = dto.DisplayOrder, ShowOnHomepage = dto.ShowOnHomepage,
        MetaKeywords = dto.MetaKeywords, MetaDescription = dto.MetaDescription, MetaTitle = dto.MetaTitle
    };

    private static object ToArticleRequest(KnowledgebaseArticle a) => new {
        name = a.Name, content = a.Content, seName = a.SeName, parentCategoryId = a.ParentCategoryId,
        published = a.Published, displayOrder = a.DisplayOrder, showOnHomepage = a.ShowOnHomepage,
        metaKeywords = a.MetaKeywords, metaDescription = a.MetaDescription, metaTitle = a.MetaTitle
    };

    private static KnowledgebaseCategory ToCategoryEntity(KbCategoryClientDto dto) => new() {
        Id = dto.Id, Name = dto.Name, Description = dto.Description, SeName = dto.SeName,
        ParentCategoryId = dto.ParentCategoryId, Published = dto.Published,
        DisplayOrder = dto.DisplayOrder,
        MetaKeywords = dto.MetaKeywords, MetaDescription = dto.MetaDescription, MetaTitle = dto.MetaTitle
    };

    private static object ToCategoryRequest(KnowledgebaseCategory c) => new {
        name = c.Name, description = c.Description, seName = c.SeName, parentCategoryId = c.ParentCategoryId,
        published = c.Published, displayOrder = c.DisplayOrder,
        metaKeywords = c.MetaKeywords, metaDescription = c.MetaDescription, metaTitle = c.MetaTitle
    };
}
