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
        int pageIndex = 0, int pageSize = int.MaxValue, bool ignoreAcl = false,
        bool showHidden = false, string newsTitle = "")
    {
        var url = $"api/news?storeId={storeId}&pageIndex={pageIndex}&pageSize={pageSize}&showHidden={showHidden}&newsTitle={newsTitle}";
        var items = await _http.GetFromJsonAsync<List<NewsItemClientDto>>(url) ?? new List<NewsItemClientDto>();
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
        if (dto == null) throw new InvalidOperationException("API returned null response for InsertNews");
        news.Id = dto.Id;
    }

    public async Task UpdateNews(NewsItem news)
    {
        var response = await _http.PutAsJsonAsync($"api/news/{news.Id}", ToRequest(news));
        response.EnsureSuccessStatusCode();
        _cache.Remove($"cms:news:{news.Id}");
    }

    public async Task DeleteNews(NewsItem newsItem)
    {
        var response = await _http.DeleteAsync($"api/news/{newsItem.Id}");
        response.EnsureSuccessStatusCode();
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
