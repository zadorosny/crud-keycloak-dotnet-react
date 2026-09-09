import { OTLPTraceExporter } from "@opentelemetry/exporter-trace-otlp-proto";
import { registerInstrumentations } from "@opentelemetry/instrumentation";
import { FetchInstrumentation } from "@opentelemetry/instrumentation-fetch";
import { resourceFromAttributes } from "@opentelemetry/resources";
import { BatchSpanProcessor, WebTracerProvider } from "@opentelemetry/sdk-trace-web";
import { ATTR_SERVICE_NAME, ATTR_SERVICE_VERSION } from "@opentelemetry/semantic-conventions";
import { config } from "./config";

/**
 * Traces do browser exportados por OTLP/HTTP em protobuf — o coletor recusa JSON com 415.
 * Só liga quando há um endpoint configurado
 * (`VITE_OTEL_EXPORTER_OTLP_ENDPOINT`), então um build sem coletor não tenta exportar nada.
 *
 * O ponto de ligar isso é a continuidade: o `traceparent` viaja junto das chamadas para a API, e o
 * span do clique no navegador aparece na mesma trace do span do servidor e do comando SQL.
 */
export function startTelemetry(): void {
  const endpoint = config.otlpEndpoint;
  if (!endpoint) {
    return;
  }

  const provider = new WebTracerProvider({
    resource: resourceFromAttributes({
      [ATTR_SERVICE_NAME]: "mfacrud-web",
      [ATTR_SERVICE_VERSION]: "1.0.0",
    }),
    spanProcessors: [new BatchSpanProcessor(new OTLPTraceExporter({ url: `${endpoint}/v1/traces` }))],
  });

  provider.register();

  registerInstrumentations({
    tracerProvider: provider,
    instrumentations: [
      new FetchInstrumentation({
        // Sem isso o header de trace não é anexado em chamadas cross-origin, que é o caso aqui
        // (o front está em :5173 e a API em :5080).
        propagateTraceHeaderCorsUrls: [new RegExp(escapeForRegExp(config.apiUrl))],
        ignoreUrls: [new RegExp(escapeForRegExp(endpoint))],
      }),
    ],
  });
}

function escapeForRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}
