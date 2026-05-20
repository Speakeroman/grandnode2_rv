namespace Grand.Storage.Api.DTOs;

public record PictureDto(
    string Id,
    string MimeType,
    string SeoFilename,
    string AltAttribute,
    string TitleAttribute,
    string Style,
    string ExtraField,
    bool IsNew,
    int ReferenceId,
    string ObjectId);
