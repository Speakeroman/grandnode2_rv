using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Storage.Services;
using Grand.Data;
using Grand.Data.Mongo;
using Grand.Domain.Media;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.SharedKernel.Extensions;
using Grand.Storage.Api;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Bootstrap MongoDB from Aspire-injected connection string
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
builder.Services.AddScoped<IStoreFilesContext, MongoStoreFilesContext>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));

builder.Services.AddSingleton<IAuditInfoProvider, ServiceAuditInfoProvider>();

// Cache — MemoryCacheBase requires IMediator and CacheConfig
builder.Services.AddMemoryCache();
builder.Services.AddSingleton(new CacheConfig { DefaultCacheTimeMinutes = 60 });
builder.Services.AddMediatR(options =>
    options.RegisterServicesFromAssembly(typeof(PictureService).Assembly));
builder.Services.AddScoped<ICacheBase, MemoryCacheBase>();

// Settings
var mediaSettings = new MediaSettings();
builder.Configuration.GetSection("Media").Bind(mediaSettings);
builder.Services.AddSingleton(mediaSettings);

var storageSettings = new StorageSettings();
builder.Configuration.GetSection("Storage").Bind(storageSettings);
builder.Services.AddSingleton(storageSettings);

// Picture service — select implementation based on cloud config
var azureConfig = new AzureConfig();
builder.Configuration.GetSection("Azure").Bind(azureConfig);
var amazonConfig = new AmazonConfig();
builder.Configuration.GetSection("Amazon").Bind(amazonConfig);

var useAzure = !string.IsNullOrEmpty(azureConfig.AzureBlobStorageConnectionString);
var useAmazon = !string.IsNullOrEmpty(amazonConfig.AmazonAwsAccessKeyId)
    && !string.IsNullOrEmpty(amazonConfig.AmazonAwsSecretAccessKey)
    && !string.IsNullOrEmpty(amazonConfig.AmazonBucketName)
    && !string.IsNullOrEmpty(amazonConfig.AmazonRegion);

if (useAzure)
    builder.Services.AddScoped<IPictureService, AzurePictureService>();
else if (useAmazon)
    builder.Services.AddScoped<IPictureService, AmazonPictureService>();
else
    builder.Services.AddScoped<IPictureService, PictureService>();

builder.Services.AddScoped<IMediaFileStore>(sp => {
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var param = builder.Configuration[CommonPath.DirectoryParam];
    var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
    var fileStore = new FileSystemStore(Path.Combine(webRoot, param ?? ""));
    return new DefaultMediaFileStore(fileStore);
});

builder.Services.AddScoped<IDownloadService, DownloadService>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseStaticFiles();
app.MapControllers();

await app.RunAsync();
