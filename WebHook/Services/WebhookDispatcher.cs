using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Threading.Channels;
using WebHook.Data;
using WebHook.Models;
using WebHook.OpenTelemetry;

namespace WebHook.Services;

internal sealed class WebhookDispatcher(
    Channel<WebhookDispatch> webhookChannel,
    IHttpClientFactory httpClientFactory,
    WebhooksDbContext dbContext)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly WebhooksDbContext _dbContext = dbContext;

    public async Task DispatchAsync<T>(string eventType, T data)
        where T : notnull
    {
        using Activity? activity = DiagnosticConfig.Source.StartActivity($"{eventType} dispatch webhook");
        activity?.AddTag("event.type", eventType);
        await webhookChannel.Writer.WriteAsync(new WebhookDispatch(eventType, data, activity?.Id));
    }

    public async Task ProcessAsync<T>(string eventType, T data)
    {
        // 1. Находим все подписки на данный тип события
        var subscriptions = await _dbContext.WebhookSubscriptions
            .AsNoTracking()
            .Where(s => s.EventType == eventType)
            .ToListAsync();

        // 2. Рассылаем запросы всем подписчикам
        foreach (WebhookSubscription webhookSebscription in subscriptions)
        {
            using var httpClient = _httpClientFactory.CreateClient();

            // Формируем стандартную обертку (Envelope) для вебхука
            var payload = new WebhookPayload<T>(
                Guid.NewGuid(),
                webhookSebscription.EventType,
                webhookSebscription.Id,
                DateTime.UtcNow,
                data // Сами полезные данные (например, заказ)
            );

            var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);

            // Отправляем асинхронный POST-запрос
            try
            {
                HttpResponseMessage response = await httpClient.PostAsJsonAsync(webhookSebscription.WebhookUrl, payload);

                var attempt = new WebhookDeliveryAttempt
                {
                    Id = Guid.NewGuid(),
                    WebhookSubscriptionId = webhookSebscription.Id,
                    Payload = jsonPayload,
                    ResponseStatusCode = (int)response.StatusCode,
                    Success = response.IsSuccessStatusCode,
                    Timestamp = DateTime.UtcNow
                };

                _dbContext.WebhookDeliveryAttempts.Add(attempt);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                var attempt = new WebhookDeliveryAttempt
                {
                    Id = Guid.NewGuid(),
                    WebhookSubscriptionId = webhookSebscription.Id,
                    Payload = jsonPayload,
                    ResponseStatusCode = null,
                    Success = false,
                    Timestamp = DateTime.UtcNow
                };

                _dbContext.WebhookDeliveryAttempts.Add(attempt);
                await _dbContext.SaveChangesAsync();
            }

        }
    }
}