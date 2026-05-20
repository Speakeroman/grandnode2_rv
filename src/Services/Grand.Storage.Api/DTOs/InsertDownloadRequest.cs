namespace Grand.Storage.Api.DTOs;

public class InsertDownloadRequest
{
    public string CustomerId { get; set; }
    public bool UseDownloadUrl { get; set; }
    public string DownloadUrl { get; set; }
    public byte[] DownloadBinary { get; set; }
    public string ContentType { get; set; }
    public string Filename { get; set; }
    public string Extension { get; set; }
    public int DownloadType { get; set; }
    public string ReferenceId { get; set; }
}
