namespace WebHook.Contracts;

public sealed record WebhookDispatched(string EventType, object Data);
