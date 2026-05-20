using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain;
using Grand.Domain.Common;
using Grand.Domain.Media;
using Grand.Storage.Client.DTOs;
using Microsoft.Extensions.Caching.Memory;
using System.Linq.Expressions;
using System.Net.Http.Json;
using System.Reflection;

namespace Grand.Storage.Client.Services;

internal sealed class StorageApiPictureService : IPictureService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;

    private static readonly TimeSpan UrlCacheDuration = TimeSpan.FromMinutes(60);

    public StorageApiPictureService(IHttpClientFactory factory, IMemoryCache cache)
    {
        _http = factory.CreateClient(StorageClientConstants.HttpClientName);
        _cache = cache;
    }

    public async Task<byte[]> LoadPictureBinary(Picture picture)
    {
        var response = await _http.GetAsync($"api/pictures/{picture.Id}/binary");
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<byte[]> LoadPictureBinary(Picture picture, bool fromDb)
    {
        return await LoadPictureBinary(picture);
    }

    public string GetPictureSeName(string name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty;
        return new string(name.ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_')
            .ToArray());
    }

    public async Task<string> GetDefaultPictureUrl(int targetSize = 0, string storeLocation = null)
    {
        var cacheKey = $"storage.default-url.{targetSize}";
        if (_cache.TryGetValue(cacheKey, out string url))
            return url;

        var qs = BuildQueryString(("targetSize", targetSize.ToString()),
            ("storeLocation", storeLocation));
        var response = await _http.GetFromJsonAsync<UrlResponse>($"api/pictures/default-url{qs}");
        url = response?.Url;
        _cache.Set(cacheKey, url, UrlCacheDuration);
        return url;
    }

    public async Task<string> GetPictureUrl(string pictureId,
        int targetSize = 0,
        bool showDefaultPicture = true,
        string storeLocation = null)
    {
        var cacheKey = $"storage.url.{pictureId}.{targetSize}.{showDefaultPicture}";
        if (_cache.TryGetValue(cacheKey, out string url))
            return url;

        var qs = BuildQueryString(
            ("targetSize", targetSize.ToString()),
            ("showDefaultPicture", showDefaultPicture.ToString()),
            ("storeLocation", storeLocation));
        var response = await _http.GetFromJsonAsync<UrlResponse>($"api/pictures/{pictureId}/url{qs}");
        url = response?.Url;
        _cache.Set(cacheKey, url, UrlCacheDuration);
        return url;
    }

    public async Task<string> GetPictureUrl(Picture picture,
        int targetSize = 0,
        bool showDefaultPicture = true,
        string storeLocation = null)
    {
        return await GetPictureUrl(picture.Id, targetSize, showDefaultPicture, storeLocation);
    }

    public Task<string> GetThumbPhysicalPath(Picture picture, int targetSize = 0, bool showDefaultPicture = true)
    {
        return Task.FromResult<string>(null);
    }

    public async Task<Picture> GetPictureById(string pictureId)
    {
        var dto = await _http.GetFromJsonAsync<PictureClientDto>($"api/pictures/{pictureId}");
        return dto == null ? null : ToEntity(dto);
    }

    public async Task DeletePicture(Picture picture)
    {
        await _http.DeleteAsync($"api/pictures/{picture.Id}");
        InvalidatePictureCache(picture.Id);
    }

    public Task DeletePictureOnFileSystem(Picture picture) => Task.CompletedTask;

    public Task SavePictureInFile(string pictureId, byte[] pictureBinary, string mimeType) => Task.CompletedTask;

    public async Task ClearThumbs()
    {
        await _http.PostAsync("api/pictures/clear-thumbs", null);
    }

    public IPagedList<Picture> GetPictures(int pageIndex = 0, int pageSize = int.MaxValue)
    {
        return GetPicturesAsync(pageIndex, pageSize).GetAwaiter().GetResult();
    }

    private async Task<IPagedList<Picture>> GetPicturesAsync(int pageIndex, int pageSize)
    {
        var dto = await _http.GetFromJsonAsync<PagedPictureClientDto>(
            $"api/pictures?pageIndex={pageIndex}&pageSize={pageSize}");
        if (dto == null) return new PagedList<Picture>([], pageIndex, pageSize, 0);
        var items = dto.Items.Select(ToEntity).ToList();
        return new PagedList<Picture>(items, dto.PageIndex, dto.PageSize, dto.TotalCount);
    }

    public async Task<Picture> InsertPicture(
        byte[] pictureBinary, string mimeType, string seoFilename,
        string altAttribute = null, string titleAttribute = null,
        bool isNew = true, Reference reference = Reference.None,
        string objectId = "", bool validateBinary = false)
    {
        var request = new {
            pictureBinary,
            mimeType,
            seoFilename,
            altAttribute,
            titleAttribute,
            isNew,
            referenceId = (int)reference,
            objectId,
            validateBinary
        };
        var response = await _http.PostAsJsonAsync("api/pictures", request);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<PictureClientDto>();
        return ToEntity(dto);
    }

    public async Task<Picture> UpdatePicture(
        string pictureId, byte[] pictureBinary, string mimeType,
        string seoFilename, string altAttribute = null, string titleAttribute = null,
        string style = null, string extraField = null,
        bool isNew = true, bool validateBinary = true)
    {
        var request = new {
            pictureBinary,
            mimeType,
            seoFilename,
            altAttribute,
            titleAttribute,
            style,
            extraField,
            isNew,
            validateBinary
        };
        var response = await _http.PutAsJsonAsync($"api/pictures/{pictureId}", request);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<PictureClientDto>();
        InvalidatePictureCache(pictureId);
        return ToEntity(dto);
    }

    public async Task<Picture> UpdatePicture(Picture picture)
    {
        var dto = new {
            id = picture.Id,
            mimeType = picture.MimeType,
            seoFilename = picture.SeoFilename,
            altAttribute = picture.AltAttribute,
            titleAttribute = picture.TitleAttribute,
            style = picture.Style,
            extraField = picture.ExtraField,
            isNew = picture.IsNew,
            referenceId = (int)picture.Reference,
            objectId = picture.ObjectId
        };
        var response = await _http.PutAsJsonAsync($"api/pictures/{picture.Id}/picture-object", dto);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PictureClientDto>();
        InvalidatePictureCache(picture.Id);
        return ToEntity(result);
    }

    public async Task UpdatePictureField<T>(Picture picture, Expression<Func<Picture, T>> expression, T value)
    {
        if (expression.Body is not MemberExpression memberExpr)
            return;

        var property = (PropertyInfo)memberExpr.Member;
        property.SetValue(picture, value);
        await UpdatePicture(picture);
    }

    public async Task<Picture> SetSeoFilename(Picture picture, string seoFilename)
    {
        picture.SeoFilename = seoFilename;
        return await UpdatePicture(picture);
    }

    public byte[] ValidatePicture(byte[] pictureBinary, string mimeType) => pictureBinary;

    public byte[] ConvertPicture(byte[] pictureBinary, int imageQuality, string format = "Webp") => pictureBinary;

    private void InvalidatePictureCache(string pictureId)
    {
        _cache.Remove($"storage.url.{pictureId}.");
    }

    private static Picture ToEntity(PictureClientDto dto) => new() {
        Id = dto.Id,
        MimeType = dto.MimeType,
        SeoFilename = dto.SeoFilename,
        AltAttribute = dto.AltAttribute,
        TitleAttribute = dto.TitleAttribute,
        Style = dto.Style,
        ExtraField = dto.ExtraField,
        IsNew = dto.IsNew,
        Reference = (Reference)dto.ReferenceId,
        ObjectId = dto.ObjectId
    };

    private static string BuildQueryString(params (string key, string value)[] pairs)
    {
        var parts = pairs
            .Where(p => !string.IsNullOrEmpty(p.value))
            .Select(p => $"{p.key}={Uri.EscapeDataString(p.value)}");
        var qs = string.Join("&", parts);
        return qs.Length > 0 ? "?" + qs : "";
    }

    private record UrlResponse(string Url);
}
