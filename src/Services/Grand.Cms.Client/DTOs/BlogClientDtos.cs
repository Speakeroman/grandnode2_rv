namespace Grand.Cms.Client.DTOs;

internal record BlogPostClientDto(
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
