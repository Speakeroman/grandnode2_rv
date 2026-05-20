namespace Grand.Storage.Client.DTOs;

internal record PagedPictureClientDto(
    IList<PictureClientDto> Items,
    int TotalCount,
    int PageIndex,
    int PageSize);
