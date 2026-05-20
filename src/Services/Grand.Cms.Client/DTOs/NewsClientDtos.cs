namespace Grand.Cms.Client.DTOs;

internal record NewsItemClientDto(
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
