using Grand.Business.Core.Interfaces.Cms;
using Grand.Cms.Api.DTOs;
using Grand.Domain.Knowledgebase;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Cms.Api.Controllers;

[ApiController]
[Route("api/knowledgebase")]
public class KnowledgebaseController : ControllerBase
{
    private readonly IKnowledgebaseService _kb;

    public KnowledgebaseController(IKnowledgebaseService kb) => _kb = kb;

    [HttpGet("articles")]
    public async Task<IActionResult> GetArticles([FromQuery] string storeId = "")
    {
        var articles = await _kb.GetKnowledgebaseArticles(storeId);
        return Ok(articles.Select(ToArticleDto).ToList());
    }

    [HttpGet("articles/{id}")]
    public async Task<IActionResult> GetArticleById(string id)
    {
        var article = await _kb.GetKnowledgebaseArticle(id);
        return article == null ? NotFound() : Ok(ToArticleDto(article));
    }

    [HttpPost("articles")]
    public async Task<IActionResult> CreateArticle([FromBody] KbArticleRequest request)
    {
        var article = ArticleFromRequest(request);
        await _kb.InsertKnowledgebaseArticle(article);
        return CreatedAtAction(nameof(GetArticleById), new { id = article.Id }, ToArticleDto(article));
    }

    [HttpPut("articles/{id}")]
    public async Task<IActionResult> UpdateArticle(string id, [FromBody] KbArticleRequest request)
    {
        var article = await _kb.GetKnowledgebaseArticle(id);
        if (article == null) return NotFound();
        ApplyArticleRequest(article, request);
        await _kb.UpdateKnowledgebaseArticle(article);
        return Ok(ToArticleDto(article));
    }

    [HttpDelete("articles/{id}")]
    public async Task<IActionResult> DeleteArticle(string id)
    {
        var article = await _kb.GetKnowledgebaseArticle(id);
        if (article == null) return NotFound();
        await _kb.DeleteKnowledgebaseArticle(article);
        return NoContent();
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _kb.GetKnowledgebaseCategories();
        return Ok(categories.Select(ToCategoryDto).ToList());
    }

    [HttpGet("categories/{id}")]
    public async Task<IActionResult> GetCategoryById(string id)
    {
        var category = await _kb.GetKnowledgebaseCategory(id);
        return category == null ? NotFound() : Ok(ToCategoryDto(category));
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] KbCategoryRequest request)
    {
        var category = CategoryFromRequest(request);
        await _kb.InsertKnowledgebaseCategory(category);
        return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id }, ToCategoryDto(category));
    }

    [HttpPut("categories/{id}")]
    public async Task<IActionResult> UpdateCategory(string id, [FromBody] KbCategoryRequest request)
    {
        var category = await _kb.GetKnowledgebaseCategory(id);
        if (category == null) return NotFound();
        ApplyCategoryRequest(category, request);
        await _kb.UpdateKnowledgebaseCategory(category);
        return Ok(ToCategoryDto(category));
    }

    [HttpDelete("categories/{id}")]
    public async Task<IActionResult> DeleteCategory(string id)
    {
        var category = await _kb.GetKnowledgebaseCategory(id);
        if (category == null) return NotFound();
        await _kb.DeleteKnowledgebaseCategory(category);
        return NoContent();
    }

    private static KbArticleDto ToArticleDto(KnowledgebaseArticle a) => new(
        a.Id, a.Name, a.Content, a.SeName, a.ParentCategoryId, a.Published,
        a.DisplayOrder, a.ShowOnHomepage, a.MetaKeywords, a.MetaDescription, a.MetaTitle);

    private static KnowledgebaseArticle ArticleFromRequest(KbArticleRequest r) => new() {
        Name = r.Name, Content = r.Content, SeName = r.SeName, ParentCategoryId = r.ParentCategoryId,
        Published = r.Published, DisplayOrder = r.DisplayOrder, ShowOnHomepage = r.ShowOnHomepage,
        MetaKeywords = r.MetaKeywords, MetaDescription = r.MetaDescription, MetaTitle = r.MetaTitle
    };

    private static void ApplyArticleRequest(KnowledgebaseArticle a, KbArticleRequest r) {
        a.Name = r.Name; a.Content = r.Content; a.SeName = r.SeName; a.ParentCategoryId = r.ParentCategoryId;
        a.Published = r.Published; a.DisplayOrder = r.DisplayOrder; a.ShowOnHomepage = r.ShowOnHomepage;
        a.MetaKeywords = r.MetaKeywords; a.MetaDescription = r.MetaDescription; a.MetaTitle = r.MetaTitle;
    }

    private static KbCategoryDto ToCategoryDto(KnowledgebaseCategory c) => new(
        c.Id, c.Name, c.Description, c.SeName, c.ParentCategoryId, c.Published,
        c.DisplayOrder, c.MetaKeywords, c.MetaDescription, c.MetaTitle);

    private static KnowledgebaseCategory CategoryFromRequest(KbCategoryRequest r) => new() {
        Name = r.Name, Description = r.Description, SeName = r.SeName, ParentCategoryId = r.ParentCategoryId,
        Published = r.Published, DisplayOrder = r.DisplayOrder,
        MetaKeywords = r.MetaKeywords, MetaDescription = r.MetaDescription, MetaTitle = r.MetaTitle
    };

    private static void ApplyCategoryRequest(KnowledgebaseCategory c, KbCategoryRequest r) {
        c.Name = r.Name; c.Description = r.Description; c.SeName = r.SeName; c.ParentCategoryId = r.ParentCategoryId;
        c.Published = r.Published; c.DisplayOrder = r.DisplayOrder;
        c.MetaKeywords = r.MetaKeywords; c.MetaDescription = r.MetaDescription; c.MetaTitle = r.MetaTitle;
    }
}
