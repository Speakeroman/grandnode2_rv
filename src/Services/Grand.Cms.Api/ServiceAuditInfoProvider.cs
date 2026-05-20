using Grand.Data;

namespace Grand.Cms.Api;

public sealed class ServiceAuditInfoProvider : IAuditInfoProvider
{
    public string GetCurrentUser() => "cms-service";
    public DateTime GetCurrentDateTime() => DateTime.UtcNow;
}
