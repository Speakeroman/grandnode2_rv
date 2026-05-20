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
