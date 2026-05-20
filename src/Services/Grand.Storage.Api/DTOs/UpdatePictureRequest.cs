namespace Grand.Storage.Api.DTOs;

public class UpdatePictureRequest
{
    public byte[] PictureBinary { get; set; }
    public string MimeType { get; set; }
    public string SeoFilename { get; set; }
    public string AltAttribute { get; set; }
    public string TitleAttribute { get; set; }
    public string Style { get; set; }
    public string ExtraField { get; set; }
    public bool IsNew { get; set; } = true;
    public bool ValidateBinary { get; set; } = true;
}
