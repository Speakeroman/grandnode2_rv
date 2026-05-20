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
