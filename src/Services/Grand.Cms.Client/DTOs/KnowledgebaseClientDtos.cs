namespace Grand.Cms.Client.DTOs;

internal record KbArticleClientDto(
    string Id,
    string Name,
    string Content,
    string SeName,
    string ParentCategoryId,
    bool Published,
    int DisplayOrder,
    bool ShowOnHomepage,
    string MetaKeywords,
    string MetaDescription,
    string MetaTitle
);

internal record KbCategoryClientDto(
    string Id,
    string Name,
    string Description,
    string SeName,
    string ParentCategoryId,
    bool Published,
    int DisplayOrder,
    string MetaKeywords,
    string MetaDescription,
    string MetaTitle
);
