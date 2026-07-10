using Microsoft.EntityFrameworkCore;
using Webhook.Processing.Models;

namespace Webhook.Processing.Data;

internal sealed class WebhooksDbContext(DbContextOptions<WebhooksDbContext> options) 
    : DbContext(options)
{
    public DbSet<WebhookSubscription> WebhookSubscriptions { get; set; }
    public DbSet<WebhookDeliveryAttempt> WebhookDeliveryAttempts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WebhookSubscription>(entity =>
        {
            entity.ToTable("subscriptions", "webhooks");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<WebhookDeliveryAttempt>(entity =>
        {
            entity.ToTable("delivery_attempts", "webhooks");
            entity.HasKey(e => e.Id);

            entity.HasOne<WebhookSubscription>()
                .WithMany()
                .HasForeignKey(e => e.WebhookSubscriptionId);
        });
    }
}
