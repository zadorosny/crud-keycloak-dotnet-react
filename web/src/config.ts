const env = import.meta.env;

export const config = {
  apiUrl: env.VITE_API_URL ?? "http://localhost:5080/api/v1",
  // Vazio desliga a telemetria do browser.
  otlpEndpoint: env.VITE_OTEL_EXPORTER_OTLP_ENDPOINT ?? "",
  keycloak: {
    url: env.VITE_KEYCLOAK_URL ?? "http://localhost:8080",
    realm: env.VITE_KEYCLOAK_REALM ?? "mfacrud",
    clientId: env.VITE_KEYCLOAK_CLIENT_ID ?? "mfacrud-web",
  },
} as const;
