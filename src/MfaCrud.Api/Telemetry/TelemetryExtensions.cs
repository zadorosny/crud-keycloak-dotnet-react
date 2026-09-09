using System.Reflection;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MfaCrud.Api.Telemetry;

public static class TelemetryExtensions
{
    /// <summary>
    /// Traces, métricas e logs em OpenTelemetry. O exportador OTLP só é ligado quando há um
    /// endpoint configurado (<c>OTEL_EXPORTER_OTLP_ENDPOINT</c>), então rodar a API sem coletor —
    /// como fazem os testes — não muda nada além de manter a instrumentação em memória.
    /// </summary>
    public static IHostApplicationBuilder AddMfaCrudTelemetry(this IHostApplicationBuilder builder)
    {
        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

        builder.Services.AddSingleton<MfaCrudTelemetry>();

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeScopes = true;
            logging.IncludeFormattedMessage = true;
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(MfaCrudTelemetry.ServiceName, serviceVersion: version)
                .AddAttributes([new KeyValuePair<string, object>("deployment.environment", builder.Environment.EnvironmentName)]))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    // O /health é chamado a cada 10s pelo healthcheck do compose e não diz nada.
                    options.Filter = context => !context.Request.Path.StartsWithSegments("/health");
                    options.RecordException = true;
                })
                .AddHttpClientInstrumentation()
                // O provider Npgsql publica a própria ActivitySource com o SQL de cada comando.
                .AddSource("Npgsql"))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(MfaCrudTelemetry.MeterName)
                .AddMeter("Npgsql"));

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }
}
