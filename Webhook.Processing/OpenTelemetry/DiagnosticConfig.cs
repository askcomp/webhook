using System.Diagnostics;

namespace Webhook.Processing.OpenTelemetry;

public static class DiagnosticConfig
{
    public static readonly ActivitySource Source = new("webhooks-processing");
}
