using MassTransit;
using Microsoft.EntityFrameworkCore;
using Webhook.Processing.Data;
using WebHook.Contracts;

namespace Webhook.Processing.Services;

public sealed record WebhookTriggered(
    Guid SubscriptionId,
    string EventType,
    string WebhookUrl,
    object Data
);

internal sealed class WebhookDispatchedConsumer(
    WebhooksDbContext dbContext)
        : IConsumer<WebhookDispatched>
{
    public async Task Consume(ConsumeContext<WebhookDispatched> context)
    {
        var message = context.Message;

        var subscriptions = await dbContext.WebhookSubscriptions
            .AsNoTracking()
            .Where(s => s.EventType == message.EventType)
            .ToListAsync();

        foreach (var subscription in subscriptions)
        {
            await context.Publish(new WebhookTriggered(
                subscription.Id,
                subscription.EventType,
                subscription.WebhookUrl,
                message.Data
            ));
        }
    }
}