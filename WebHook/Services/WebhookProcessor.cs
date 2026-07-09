using System.Diagnostics;
using System.Threading.Channels;
using WebHook.OpenTelemetry;

namespace WebHook.Services;

internal sealed class WebhookProcessor(
    IServiceScopeFactory serviceScopeFactory,
    Channel<WebhookDispatch> webhookChannel) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var dispatch in webhookChannel.Reader.ReadAllAsync(stoppingToken))
        {
            using Activity? activity = DiagnosticConfig.Source.StartActivity(
                $"{dispatch.EventType} process webhook",
                ActivityKind.Internal,
                parentId: dispatch.ParentActivityId);

            using var scope = serviceScopeFactory.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<WebhookDispatcher>();

            await dispatcher.ProcessAsync(dispatch.EventType, dispatch.Data);
        }
    }
}
