namespace Aspire.AppHost;

public static class ProjectConfiguration
{
    public static IResourceBuilder<ProjectResource> ConfigureGrandWebProject(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> mongodb,
        IResourceBuilder<ProjectResource> storageApi = null)
    {
        var project = builder
            .AddProject<Projects.Grand_Web>("grand-web")
            .WithHttpEndpoint(80, name: "front")
            .WithReference(mongodb);

        if (storageApi != null)
            project = project.WithReference(storageApi).WaitFor(storageApi);

        return project;
    }

    public static IResourceBuilder<ProjectResource> ConfigureGrandStorageApiProject(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> mongodb)
    {
        return builder
            .AddProject<Projects.Grand_Storage_Api>("grand-storage-api")
            .WithHttpEndpoint(5100, name: "storage")
            .WithReference(mongodb);
    }

    public static void ConfigureGrandGatewayProject(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> grandWeb)
    {
        builder
            .AddProject<Projects.Grand_Gateway>("grand-gateway")
            .WithHttpEndpoint(8080, name: "gateway")
            .WithReference(grandWeb)
            .WaitFor(grandWeb);
    }
}