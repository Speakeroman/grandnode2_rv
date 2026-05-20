namespace Grand.Cms.Client.DTOs;

internal record PageClientDto(
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
