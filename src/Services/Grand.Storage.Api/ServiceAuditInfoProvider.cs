using Grand.Data;

namespace Grand.Storage.Api;

public sealed class ServiceAuditInfoProvider : IAuditInfoProvider
{
    public string GetCurrentUser() => "storage-service";
    public DateTime GetCurrentDateTime() => DateTime.UtcNow;
}
