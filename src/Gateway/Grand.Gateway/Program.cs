var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Aspire injects services__grand-web__http__0 when WithReference(grandWeb) is configured.
// Fall back to the appsettings value for local development without Aspire.
var grandWebAddress =
    builder.Configuration["services__grand-web__http__0"]
    ?? builder.Configuration["services__grand-web__https__0"]
    ?? builder.Configuration["GrandWebAddress"]
    ?? "http://localhost:5000";

builder.Configuration["ReverseProxy:Clusters:grand-web:Destinations:default:Address"] = grandWebAddress;

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapReverseProxy();

await app.RunAsync();
