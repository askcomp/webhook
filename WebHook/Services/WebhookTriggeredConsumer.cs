using MassTransit;
using Microsoft.EntityFrameworkCore;
using WebHook.Data;
using WebHook.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebHook.Services;

internal sealed class WebhookTriggeredConsumer(
    IHttpClientFactory httpClientFactory,
    WebhooksDbContext dbContext) 
    : IConsumer<WebhookTriggered>
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly WebhooksDbContext _dbContext = dbContext;

    public async Task Consume(ConsumeContext<WebhookTriggered> context)
    {
        using var httpClient = _httpClientFactory.CreateClient();

        // Формируем стандартную обертку (Envelope) для вебхука
        var payload = new WebhookPayload()
        {
            Id = Guid.NewGuid(),
            EventType = context.Message.EventType,
            SubscriptionId = context.Message.SubscriptionId,
            Timestamp = DateTime.UtcNow,
            Data = context.Message.Data
        };

        var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);

        // Отправляем асинхронный POST-запрос
        try
        {
            HttpResponseMessage response = await httpClient.PostAsJsonAsync(context.Message.WebhookUrl, payload);
            response.EnsureSuccessStatusCode();

            var attempt = new WebhookDeliveryAttempt
            {
                Id = Guid.NewGuid(),
                WebhookSubscriptionId = context.Message.SubscriptionId,
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
                WebhookSubscriptionId = context.Message.SubscriptionId,
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
