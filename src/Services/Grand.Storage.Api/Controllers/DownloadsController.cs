using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Media;
using Grand.Storage.Api.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Storage.Api.Controllers;

[ApiController]
[Route("api/downloads")]
public class DownloadsController : ControllerBase
{
    private readonly IDownloadService _downloadService;

    public DownloadsController(IDownloadService downloadService)
    {
        _downloadService = downloadService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DownloadDto>> GetById(string id)
    {
        var download = await _downloadService.GetDownloadById(id);
        if (download == null) return NotFound();
        return ToDto(download);
    }

    [HttpGet("by-guid/{guid}")]
    public async Task<ActionResult<DownloadDto>> GetByGuid(Guid guid)
    {
        var download = await _downloadService.GetDownloadByGuid(guid);
        if (download == null) return NotFound();
        return ToDto(download);
    }

    [HttpPost]
    public async Task<ActionResult<DownloadDto>> Insert([FromBody] InsertDownloadRequest request)
    {
        var download = new Download {
            CustomerId = request.CustomerId,
            DownloadGuid = Guid.NewGuid(),
            UseDownloadUrl = request.UseDownloadUrl,
            DownloadUrl = request.DownloadUrl,
            DownloadBinary = request.DownloadBinary,
            ContentType = request.ContentType,
            Filename = request.Filename,
            Extension = request.Extension,
            DownloadType = (DownloadType)request.DownloadType,
            ReferenceId = request.ReferenceId
        };
        await _downloadService.InsertDownload(download);
        return CreatedAtAction(nameof(GetById), new { id = download.Id }, ToDto(download));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<DownloadDto>> Update(string id, [FromBody] UpdateDownloadRequest request)
    {
        var download = await _downloadService.GetDownloadById(id);
        if (download == null) return NotFound();
        download.CustomerId = request.CustomerId;
        download.UseDownloadUrl = request.UseDownloadUrl;
        download.DownloadUrl = request.DownloadUrl;
        download.DownloadBinary = request.DownloadBinary;
        download.ContentType = request.ContentType;
        download.Filename = request.Filename;
        download.Extension = request.Extension;
        download.DownloadType = (DownloadType)request.DownloadType;
        download.ReferenceId = request.ReferenceId;
        await _downloadService.UpdateDownload(download);
        return ToDto(download);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var download = await _downloadService.GetDownloadById(id);
        if (download == null) return NotFound();
        await _downloadService.DeleteDownload(download);
        return NoContent();
    }

    private static DownloadDto ToDto(Download d) => new(
        d.Id, d.CustomerId, d.DownloadGuid, d.UseDownloadUrl, d.DownloadUrl,
        d.DownloadBinary, d.ContentType, d.Filename, d.Extension,
        (int)d.DownloadType, d.ReferenceId);
}
