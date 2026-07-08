using Microsoft.EntityFrameworkCore;
using WebHook.Data;
using WebHook.Extentions;
using WebHook.Models;
using WebHook.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//builder.Services.AddSingleton<InMemoryOrderRepository>();
//builder.Services.AddSingleton<InMemoryWebhookSubscriptionRepository>();

builder.Services.AddHttpClient();
builder.Services.AddScoped<WebhookDispatcher>();

builder.Services.AddDbContext<WebhooksDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("webhooks")));

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Open API v1");
    });

    app.ApplyMigrations();

}

app.UseHttpsRedirection();

// 1. Эндпоинт для регистрации подписки клиентом
app.MapPost("webhooks/subscriptions", (
    CreateWebhookRequest request,
    WebhooksDbContext context) =>
{
    var subscription = new WebhookSubscription(
        Guid.NewGuid(),
        request.EventType,
        request.WebhookUrl,
        DateTime.UtcNow);

    context.WebhookSubscriptions.Add(subscription);
    context.SaveChanges();

    return Results.Ok(subscription);
});

// 2. Эндпоинт создания заказа, генерирующий событие "orderCreated"
app.MapPost("/orders", async (
    CreateOrderRequest request,
    WebhooksDbContext context,
    WebhookDispatcher webhookDispatcher) =>
{
    var order = new Order(Guid.NewGuid(), request.CustomerName, request.Amount, DateTime.UtcNow);
    context.Orders.Add(order);
    await context.SaveChangesAsync();

    // Триггерим вебхук для всех, кто подписан на событие "orderCreated"
    await webhookDispatcher.DispatchAsync("order.created", order);

    return Results.Ok(order);
})
.WithTags("Orders");


app.MapGet("orders", async (WebhooksDbContext context) =>
{
    return Results.Ok(await context.Orders.ToListAsync());
})
.WithTags("Orders");


app.Run();