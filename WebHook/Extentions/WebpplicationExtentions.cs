using Microsoft.EntityFrameworkCore;
using WebHook.Data;

namespace WebHook.Extentions;

public static class WebpplicationExtentions
{
    public static async Task ApplyMigrations(this WebApplication builder)
    {
        using var scope = builder.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WebhooksDbContext>();
        await db.Database.MigrateAsync();
    }
}
