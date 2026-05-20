namespace Grand.Cms.Api.DTOs;

public record PageDto(
    string Id,
    string SystemName,
    string Title,
    string Body,
    string SeName,
    bool Published,
    DateTime? StartDateUtc,
    DateTime? EndDateUtc,
    string MetaKeywords,
    string MetaDescription,
    string MetaTitle,
    string PageLayoutId
);

public record PageRequest(
    string SystemName,
    string Title,
    string Body,
    string SeName,
    bool Published,
    DateTime? StartDateUtc,
    DateTime? EndDateUtc,
    string MetaKeywords,
    string MetaDescription,
    string MetaTitle,
    string PageLayoutId
);
