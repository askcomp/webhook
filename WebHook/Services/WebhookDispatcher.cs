using WebHook.Repositories;

namespace WebHook.Services;

internal sealed class WebhookDispatcher
{
    private readonly HttpClient _httpClient;
    private readonly InMemoryWebhookSubscriptionRepository _subscriptionRepository;

    public WebhookDispatcher(
        HttpClient httpClient,
        InMemoryWebhookSubscriptionRepository subscriptionRepository)
    {
        _httpClient = httpClient;
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task DispatchAsync(string eventType, object payload)
    {
        // 1. Находим все подписки на данный тип события
        var subscriptions = _subscriptionRepository.GetByEventType(eventType);

        // 2. Рассылаем запросы всем подписчикам
        foreach (var subscription in subscriptions)
        {
            // Формируем стандартную обертку (Envelope) для вебхука
            var webhookPayload = new
            {
                Id = Guid.NewGuid(),
                EventType = subscription.EventType,
                SubscriptionId = subscription.Id,
                Timestamp = DateTime.UtcNow,
                Data = payload // Сами полезные данные (например, заказ)
            };

            // Отправляем асинхронный POST-запрос
            await _httpClient.PostAsJsonAsync(subscription.WebhookUrl, webhookPayload);
        }
    }
}