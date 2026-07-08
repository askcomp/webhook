namespace WebHook.Services;

internal record WebhookPayload<T>(Guid Id, string EventType, Guid SubscriptionId, DateTime Timestamp, T Data);
