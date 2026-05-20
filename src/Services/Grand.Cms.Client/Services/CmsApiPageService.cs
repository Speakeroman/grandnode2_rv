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
        var items = await _http.GetFromJsonAsync<List<PageClientDto>>(
            $"api/pages?storeId={storeId}") ?? new List<PageClientDto>();
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
        if (dto == null) throw new InvalidOperationException("API returned null response for InsertPage");
        page.Id = dto.Id;
    }

    public async Task UpdatePage(Page page)
    {
        var response = await _http.PutAsJsonAsync($"api/pages/{page.Id}", ToRequest(page));
        response.EnsureSuccessStatusCode();
        _cache.Remove($"cms:page:{page.Id}");
    }

    public async Task DeletePage(Page page)
    {
        var response = await _http.DeleteAsync($"api/pages/{page.Id}");
        response.EnsureSuccessStatusCode();
        _cache.Remove($"cms:page:{page.Id}");
    }

    private static Page ToEntity(PageClientDto dto) => new() {
        Id = dto.Id,
        SystemName = dto.SystemName,
        Title = dto.Title,
        Body = dto.Body,
        SeName = dto.SeName,
        Published = dto.Published,
        StartDateUtc = dto.StartDateUtc,
        EndDateUtc = dto.EndDateUtc,
        MetaKeywords = dto.MetaKeywords,
        MetaDescription = dto.MetaDescription,
        MetaTitle = dto.MetaTitle,
        PageLayoutId = dto.PageLayoutId
    };

    private static object ToRequest(Page p) => new {
        systemName = p.SystemName,
        title = p.Title,
        body = p.Body,
        seName = p.SeName,
        published = p.Published,
        startDateUtc = p.StartDateUtc,
        endDateUtc = p.EndDateUtc,
        metaKeywords = p.MetaKeywords,
        metaDescription = p.MetaDescription,
        metaTitle = p.MetaTitle,
        pageLayoutId = p.PageLayoutId
    };
}
