namespace Grand.Storage.Api.DTOs;

public record PagedPictureResponse(
    IList<PictureDto> Items,
    int TotalCount,
    int PageIndex,
    int PageSize);
