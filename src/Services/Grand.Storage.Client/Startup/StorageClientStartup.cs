using Grand.Business.Core.Interfaces.Storage;
using Grand.Infrastructure;
using Grand.Storage.Client.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Storage.Client.Startup;

public class StorageClientStartup : IStartupApplication
{
    public int Priority => 100;
    public bool BeforeConfigure => false;

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Aspire injects the storage service URL; fall back to explicit config or localhost
        var baseAddress =
            configuration["services__grand-storage-api__http__0"]
            ?? configuration["services__grand-storage-api__https__0"]
            ?? configuration["StorageApi:BaseAddress"]
            ?? "http://localhost:5100";

        services.AddHttpClient(StorageClientConstants.HttpClientName, client => {
            client.BaseAddress = new Uri(baseAddress);
        });

        services.AddMemoryCache();
        services.AddScoped<IPictureService, StorageApiPictureService>();
        services.AddScoped<IDownloadService, StorageApiDownloadService>();
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }
}
