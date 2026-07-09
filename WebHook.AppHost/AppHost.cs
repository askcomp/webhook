var builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<PostgresDatabaseResource> database = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin()
    .AddDatabase("webhooks");

IResourceBuilder<RabbitMQServerResource> queue = builder.AddRabbitMQ("rabbitmq")
    .WithDataVolume()
    .WithManagementPlugin();

builder.AddProject<Projects.WebHook>("webhook")
    .WithReference(database)
    .WithReference(queue)
    .WaitFor(database)
    .WaitFor(queue);

builder.Build().Run();
