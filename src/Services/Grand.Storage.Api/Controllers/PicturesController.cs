using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Common;
using Grand.Domain.Media;
using Grand.Storage.Api.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Storage.Api.Controllers;

[ApiController]
[Route("api/pictures")]
public class PicturesController : ControllerBase
{
    private readonly IPictureService _pictureService;

    public PicturesController(IPictureService pictureService)
    {
        _pictureService = pictureService;
    }

    [HttpGet]
    public ActionResult<PagedPictureResponse> GetPictures(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20)
    {
        var pictures = _pictureService.GetPictures(pageIndex, pageSize);
        var items = pictures.Select(ToDto).ToList();
        return new PagedPictureResponse(items, pictures.TotalCount, pictures.PageIndex, pictures.PageSize);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PictureDto>> GetById(string id)
    {
        var picture = await _pictureService.GetPictureById(id);
        if (picture == null) return NotFound();
        return ToDto(picture);
    }

    [HttpGet("{id}/binary")]
    public async Task<IActionResult> GetBinary(string id)
    {
        var picture = await _pictureService.GetPictureById(id);
        if (picture == null) return NotFound();
        var binary = await _pictureService.LoadPictureBinary(picture);
        if (binary == null || binary.Length == 0) return NoContent();
        return File(binary, picture.MimeType ?? "application/octet-stream");
    }

    [HttpGet("{id}/url")]
    public async Task<PictureUrlResponse> GetUrl(
        string id,
        [FromQuery] int targetSize = 0,
        [FromQuery] bool showDefaultPicture = true,
        [FromQuery] string storeLocation = null)
    {
        var url = await _pictureService.GetPictureUrl(id, targetSize, showDefaultPicture, storeLocation);
        return new PictureUrlResponse(url);
    }

    [HttpGet("default-url")]
    public async Task<PictureUrlResponse> GetDefaultUrl(
        [FromQuery] int targetSize = 0,
        [FromQuery] string storeLocation = null)
    {
        var url = await _pictureService.GetDefaultPictureUrl(targetSize, storeLocation);
        return new PictureUrlResponse(url);
    }

    [HttpPost]
    public async Task<ActionResult<PictureDto>> Insert([FromBody] InsertPictureRequest request)
    {
        var picture = await _pictureService.InsertPicture(
            request.PictureBinary,
            request.MimeType,
            request.SeoFilename,
            request.AltAttribute,
            request.TitleAttribute,
            request.IsNew,
            (Reference)request.ReferenceId,
            request.ObjectId,
            request.ValidateBinary);
        return CreatedAtAction(nameof(GetById), new { id = picture.Id }, ToDto(picture));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PictureDto>> Update(string id, [FromBody] UpdatePictureRequest request)
    {
        var picture = await _pictureService.UpdatePicture(
            id,
            request.PictureBinary,
            request.MimeType,
            request.SeoFilename,
            request.AltAttribute,
            request.TitleAttribute,
            request.Style,
            request.ExtraField,
            request.IsNew,
            request.ValidateBinary);
        if (picture == null) return NotFound();
        return ToDto(picture);
    }

    [HttpPut("{id}/picture-object")]
    public async Task<ActionResult<PictureDto>> UpdatePictureObject(string id, [FromBody] PictureDto dto)
    {
        var existing = await _pictureService.GetPictureById(id);
        if (existing == null) return NotFound();
        ApplyDto(existing, dto);
        var updated = await _pictureService.UpdatePicture(existing);
        return ToDto(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var picture = await _pictureService.GetPictureById(id);
        if (picture == null) return NotFound();
        await _pictureService.DeletePicture(picture);
        return NoContent();
    }

    [HttpPost("clear-thumbs")]
    public async Task<IActionResult> ClearThumbs()
    {
        await _pictureService.ClearThumbs();
        return NoContent();
    }

    private static PictureDto ToDto(Picture p) => new(
        p.Id, p.MimeType, p.SeoFilename, p.AltAttribute, p.TitleAttribute,
        p.Style, p.ExtraField, p.IsNew, (int)p.Reference, p.ObjectId);

    private static void ApplyDto(Picture p, PictureDto dto)
    {
        p.MimeType = dto.MimeType;
        p.SeoFilename = dto.SeoFilename;
        p.AltAttribute = dto.AltAttribute;
        p.TitleAttribute = dto.TitleAttribute;
        p.Style = dto.Style;
        p.ExtraField = dto.ExtraField;
        p.IsNew = dto.IsNew;
        p.Reference = (Reference)dto.ReferenceId;
        p.ObjectId = dto.ObjectId;
    }
}
