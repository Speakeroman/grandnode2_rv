using Grand.Business.Core.Interfaces.Cms;
using Grand.Cms.Api.DTOs;
using Grand.Domain.Blogs;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Cms.Api.Controllers;

[ApiController]
[Route("api/blogs")]
public class BlogController : ControllerBase
{
    private readonly IBlogService _blog;

    public BlogController(IBlogService blog) => _blog = blog;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string storeId = "",
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool showHidden = false,
        [FromQuery] string tag = null,
        [FromQuery] string categoryId = "")
    {
        var posts = await _blog.GetAllBlogPosts(storeId, null, null, pageIndex, pageSize, showHidden, tag, "", categoryId);
        return Ok(posts.Select(ToDto).ToList());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var post = await _blog.GetBlogPostById(id);
        return post == null ? NotFound() : Ok(ToDto(post));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BlogPostRequest request)
    {
        var post = FromRequest(request);
        await _blog.InsertBlogPost(post);
        return CreatedAtAction(nameof(GetById), new { id = post.Id }, ToDto(post));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] BlogPostRequest request)
    {
        var post = await _blog.GetBlogPostById(id);
        if (post == null) return NotFound();
        ApplyRequest(post, request);
        await _blog.UpdateBlogPost(post);
        return Ok(ToDto(post));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var post = await _blog.GetBlogPostById(id);
        if (post == null) return NotFound();
        await _blog.DeleteBlogPost(post);
        return NoContent();
    }

    private static BlogPostDto ToDto(BlogPost p) => new(
        p.Id, p.Title, p.Body, p.BodyOverview, p.Tags,
        p.StartDateUtc, p.EndDateUtc, p.MetaKeywords, p.MetaDescription,
        p.MetaTitle, p.SeName, p.PictureId, p.AllowComments);

    private static BlogPost FromRequest(BlogPostRequest r) => new() {
        Title = r.Title, Body = r.Body, BodyOverview = r.BodyOverview,
        Tags = r.Tags, StartDateUtc = r.StartDateUtc, EndDateUtc = r.EndDateUtc,
        MetaKeywords = r.MetaKeywords, MetaDescription = r.MetaDescription,
        MetaTitle = r.MetaTitle, SeName = r.SeName, PictureId = r.PictureId,
        AllowComments = r.AllowComments
    };

    private static void ApplyRequest(BlogPost p, BlogPostRequest r) {
        p.Title = r.Title; p.Body = r.Body; p.BodyOverview = r.BodyOverview;
        p.Tags = r.Tags; p.StartDateUtc = r.StartDateUtc; p.EndDateUtc = r.EndDateUtc;
        p.MetaKeywords = r.MetaKeywords; p.MetaDescription = r.MetaDescription;
        p.MetaTitle = r.MetaTitle; p.SeName = r.SeName; p.PictureId = r.PictureId;
        p.AllowComments = r.AllowComments;
    }
}
