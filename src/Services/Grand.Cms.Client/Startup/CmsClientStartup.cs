using Grand.Business.Core.Interfaces.Cms;
using Grand.Cms.Client.Services;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Cms.Client.Startup;

public class CmsClientStartup : IStartupApplication
{
    public int Priority => 100;
    public bool BeforeConfigure => false;

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var baseAddress =
            configuration["services__grand-cms-api__http__0"]
            ?? configuration["services__grand-cms-api__https__0"]
            ?? configuration["CmsApi:BaseAddress"]
            ?? "http://localhost:5200";

        services.AddHttpClient(CmsClientConstants.HttpClientName, client => {
            client.BaseAddress = new Uri(baseAddress);
        });

        services.AddMemoryCache();
        services.AddScoped<IBlogService, CmsApiBlogService>();
        services.AddScoped<INewsService, CmsApiNewsService>();
        services.AddScoped<IKnowledgebaseService, CmsApiKnowledgebaseService>();
        services.AddScoped<IPageService, CmsApiPageService>();
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }
}
