using WebHook.Models;
using WebHook.Repositories;
using WebHook.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<InMemoryOrderRepository>();
builder.Services.AddSingleton<InMemoryWebhookSubscriptionRepository>();

builder.Services.AddHttpClient<WebhookDispatcher>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Open API v1");
    });

}

app.UseHttpsRedirection();

// 1. Эндпоинт для регистрации подписки клиентом
app.MapPost("webhooks/subscriptions", (
    CreateWebhookRequest request,
    InMemoryWebhookSubscriptionRepository subscriptionRepository) =>
{
    var subscription = new WebhookSubscription(
        Guid.NewGuid(),
        request.EventType,
        request.WebhookUrl,
        DateTime.UtcNow);

    subscriptionRepository.Add(subscription);

    return Results.Ok(subscription);
});

// 2. Эндпоинт создания заказа, генерирующий событие "orderCreated"
app.MapPost("/orders", async (
    CreateOrderRequest request,
    InMemoryOrderRepository repository,
    WebhookDispatcher webhookDispatcher) =>
{ 
    var order = new Order(Guid.NewGuid(), request.CustomerName, request.Amount, DateTime.UtcNow);
    repository.Add(order);

    // Триггерим вебхук для всех, кто подписан на событие "orderCreated"
    await webhookDispatcher.DispatchAsync("order.created", order);

    return Results.Ok(order);
})
.WithTags("Orders");


app.MapGet("orders", (InMemoryOrderRepository repository) =>
{
    return Results.Ok(repository.GetAll());
})
.WithTags("Orders");


app.Run();