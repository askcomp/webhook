namespace Webhook.Processing.Models;

// Модель самой подписки на вебхук
public sealed record WebhookSubscription(
    Guid Id,
    string EventType,
    string WebhookUrl,
    DateTime CreatedOnUtc);


// DTO для запроса на создание подписки
public sealed record CreateWebhookRequest(
    string EventType,
    string WebhookUrl);
