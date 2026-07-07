using WebHook.Models;

namespace WebHook.Repositories;

internal sealed class InMemoryWebhookSubscriptionRepository
{
    private readonly List<WebhookSubscription> _subscriptions = [];

    public void Add(WebhookSubscription order)
    {
        _subscriptions.Add(order);
    }

    // Поиск всех клиентов, подписавшихся на конкретное событие (например, "order.created")
    public IReadOnlyList<WebhookSubscription> GetByEventType(string eventType)
    {
        return _subscriptions
            .Where(x => x.EventType == eventType)
            .ToList()
            .AsReadOnly();
    }
}
