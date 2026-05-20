using Aspire.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// Use external MongoDB (native install) instead of a container.
// Set ConnectionStrings__Mongodb in appsettings.json or environment to override.
var mongodb = builder.AddConnectionString("Mongodb");

// RabbitMQ is optional — remove or restore the container when Docker is available.
// var rabbitmq = builder.AddRabbitMQ("rabbitmq")
//     .WithManagementPlugin()
//     .WithLifetime(ContainerLifetime.Persistent);

var storageApi = builder.ConfigureGrandStorageApiProject(mongodb);
var grandWeb = builder.ConfigureGrandWebProject(mongodb, storageApi);
builder.ConfigureGrandGatewayProject(grandWeb);

await builder.Build().RunAsync();
