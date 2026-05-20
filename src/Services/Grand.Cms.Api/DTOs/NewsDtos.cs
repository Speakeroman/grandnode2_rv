namespace Grand.Cms.Api.DTOs;

public record NewsItemDto(
    string Id,
    string Title,
    string Short,
    string Full,
    bool Published,
    DateTime? StartDateUtc,
    DateTime? EndDateUtc,
    string MetaKeywords,
    string MetaDescription,
    string MetaTitle,
    string SeName,
    string PictureId
);

public record NewsItemRequest(
    string Title,
    string Short,
    string Full,
    bool Published,
    DateTime? StartDateUtc,
    DateTime? EndDateUtc,
    string MetaKeywords,
    string MetaDescription,
    string MetaTitle,
    string SeName,
    string PictureId
);

public record PagedNewsResponse(
    IList<NewsItemDto> Items,
    int TotalCount,
    int PageIndex,
    int PageSize
);
