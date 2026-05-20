namespace Grand.Storage.Api.DTOs;

public class InsertPictureRequest
{
    public byte[] PictureBinary { get; set; }
    public string MimeType { get; set; }
    public string SeoFilename { get; set; }
    public string AltAttribute { get; set; }
    public string TitleAttribute { get; set; }
    public bool IsNew { get; set; } = true;
    public int ReferenceId { get; set; }
    public string ObjectId { get; set; } = "";
    public bool ValidateBinary { get; set; }
}
