namespace Grand.Storage.Client.DTOs;

internal record PictureClientDto(
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
