using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Media;
using Grand.Storage.Client.DTOs;
using System.Net.Http.Json;

namespace Grand.Storage.Client.Services;

internal sealed class StorageApiDownloadService : IDownloadService
{
    private readonly HttpClient _http;

    public StorageApiDownloadService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient(StorageClientConstants.HttpClientName);
    }

    public async Task<Download> GetDownloadById(string downloadId)
    {
        var dto = await _http.GetFromJsonAsync<DownloadClientDto>($"api/downloads/{downloadId}");
        return dto == null ? null : ToEntity(dto);
    }

    public async Task<Download> GetDownloadByGuid(Guid downloadGuid)
    {
        var dto = await _http.GetFromJsonAsync<DownloadClientDto>($"api/downloads/by-guid/{downloadGuid}");
        return dto == null ? null : ToEntity(dto);
    }

    public async Task InsertDownload(Download download)
    {
        var request = new {
            customerId = download.CustomerId,
            useDownloadUrl = download.UseDownloadUrl,
            downloadUrl = download.DownloadUrl,
            downloadBinary = download.DownloadBinary,
            contentType = download.ContentType,
            filename = download.Filename,
            extension = download.Extension,
            downloadType = (int)download.DownloadType,
            referenceId = download.ReferenceId
        };
        var response = await _http.PostAsJsonAsync("api/downloads", request);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<DownloadClientDto>();
        download.Id = dto!.Id;
        download.DownloadGuid = dto.DownloadGuid;
    }

    public async Task UpdateDownload(Download download)
    {
        var request = new {
            customerId = download.CustomerId,
            useDownloadUrl = download.UseDownloadUrl,
            downloadUrl = download.DownloadUrl,
            downloadBinary = download.DownloadBinary,
            contentType = download.ContentType,
            filename = download.Filename,
            extension = download.Extension,
            downloadType = (int)download.DownloadType,
            referenceId = download.ReferenceId
        };
        var response = await _http.PutAsJsonAsync($"api/downloads/{download.Id}", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteDownload(Download download)
    {
        await _http.DeleteAsync($"api/downloads/{download.Id}");
    }

    private static Download ToEntity(DownloadClientDto dto) => new() {
        Id = dto.Id,
        CustomerId = dto.CustomerId,
        DownloadGuid = dto.DownloadGuid,
        UseDownloadUrl = dto.UseDownloadUrl,
        DownloadUrl = dto.DownloadUrl,
        DownloadBinary = dto.DownloadBinary,
        ContentType = dto.ContentType,
        Filename = dto.Filename,
        Extension = dto.Extension,
        DownloadType = (DownloadType)dto.DownloadType,
        ReferenceId = dto.ReferenceId
    };
}
