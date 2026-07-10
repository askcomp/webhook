using MassTransit;
using System.Diagnostics;
using WebHook.Contracts;
using WebHook.OpenTelemetry;

namespace WebHook.Services;

internal sealed class WebhookDispatcher(
    IPublishEndpoint publishEndpoint)
{
    public async Task DispatchAsync<T>(string eventType, T data)
        where T : notnull
    {
        using Activity? activity = DiagnosticConfig.Source.StartActivity($"{eventType} dispatch webhook");
        activity?.AddTag("event.type", eventType);
        await publishEndpoint.Publish(new WebhookDispatched(eventType, data));
    }
}