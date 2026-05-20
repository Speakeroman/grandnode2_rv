namespace Grand.Cms.Api.DTOs;

public record BlogPostDto(
    string Id,
    string Title,
    string Body,
    string BodyOverview,
    string Tags,
    DateTime? StartDateUtc,
    DateTime? EndDateUtc,
    string MetaKeywords,
    string MetaDescription,
    string MetaTitle,
    string SeName,
    string PictureId,
    bool AllowComments
);

public record BlogPostRequest(
    string Title,
    string Body,
    string BodyOverview,
    string Tags,
    DateTime? StartDateUtc,
    DateTime? EndDateUtc,
    string MetaKeywords,
    string MetaDescription,
    string MetaTitle,
    string SeName,
    string PictureId,
    bool AllowComments
);

public record PagedBlogResponse(
    IList<BlogPostDto> Items,
    int TotalCount,
    int PageIndex,
    int PageSize
);
