using Grand.Business.Cms.Services;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Cms.Api;
using Grand.Data;
using Grand.Data.Mongo;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.SharedKernel.Extensions;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration[SettingsConstants.ConnectionStrings];
DataSettingsManager.Initialize(
    Path.Combine(builder.Environment.ContentRootPath, CommonPath.AppData, CommonPath.SettingsFile));
if (!string.IsNullOrEmpty(connectionString))
    DataSettingsManager.Instance.LoadDataSettings(new DataSettings {
        ConnectionString = connectionString,
        DbProvider = DbProvider.MongoDB
    });

var mongoUrl = new MongoUrl(connectionString);
var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
builder.Services.AddScoped(_ => new MongoClient(clientSettings).GetDatabase(mongoUrl.DatabaseName));
builder.Services.AddScoped<IDatabaseContext, MongoDBContext>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));

builder.Services.AddSingleton<IAuditInfoProvider, ServiceAuditInfoProvider>();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton(new CacheConfig { DefaultCacheTimeMinutes = 60 });
builder.Services.AddMediatR(options =>
    options.RegisterServicesFromAssembly(typeof(BlogService).Assembly));
builder.Services.AddScoped<ICacheBase, MemoryCacheBase>();

builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddScoped<IKnowledgebaseService, KnowledgebaseService>();
builder.Services.AddScoped<IPageService, PageService>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseStaticFiles();
app.MapControllers();

await app.RunAsync();
