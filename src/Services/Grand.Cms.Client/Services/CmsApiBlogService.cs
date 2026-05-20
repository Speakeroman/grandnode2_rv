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
        if (dto == null) throw new InvalidOperationException("API returned null response for InsertBlogPost");
        blogPost.Id = dto.Id;
    }

    public async Task UpdateBlogPost(BlogPost blogPost)
    {
        var response = await _http.PutAsJsonAsync($"api/blogs/{blogPost.Id}", ToRequest(blogPost));
        response.EnsureSuccessStatusCode();
        _cache.Remove($"cms:blog:{blogPost.Id}");
    }

    public async Task DeleteBlogPost(BlogPost blogPost)
    {
        var response = await _http.DeleteAsync($"api/blogs/{blogPost.Id}");
        response.EnsureSuccessStatusCode();
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
