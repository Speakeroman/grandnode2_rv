namespace Grand.Cms.Api.DTOs;

public record KbArticleDto(
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

public record KbArticleRequest(
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

public record KbCategoryDto(
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

public record KbCategoryRequest(
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
