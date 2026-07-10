using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Webhook.Processing.Data;
using Webhook.Processing.OpenTelemetry;
using Webhook.Processing.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.Services.AddDbContext<WebhooksDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("webhooks")));

builder.Services.AddMassTransit(busConfig =>
{
    busConfig.SetKebabCaseEndpointNameFormatter();

    busConfig.AddConsumer<WebhookDispatchedConsumer>();
    busConfig.AddConsumer<WebhookTriggeredConsumer>();

    busConfig.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("rabbitmq"));
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(DiagnosticConfig.Source.Name)
        .AddSource(MassTransit.Logging.DiagnosticHeaders.DefaultListenerName)
        .AddNpgsql());

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();

