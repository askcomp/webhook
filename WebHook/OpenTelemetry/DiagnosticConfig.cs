using System.Diagnostics;

namespace WebHook.OpenTelemetry;

public static class DiagnosticConfig
{
    public static readonly ActivitySource Source = new("webhooks-api");
}
