namespace Grand.Storage.Client.DTOs;

internal record DownloadClientDto(
    string Id,
    string CustomerId,
    Guid DownloadGuid,
    bool UseDownloadUrl,
    string DownloadUrl,
    byte[] DownloadBinary,
    string ContentType,
    string Filename,
    string Extension,
    int DownloadType,
    string ReferenceId);
