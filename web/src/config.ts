const env = import.meta.env;

export const config = {
  apiUrl: env.VITE_API_URL ?? "http://localhost:5080/api/v1",
  keycloak: {
    url: env.VITE_KEYCLOAK_URL ?? "http://localhost:8080",
    realm: env.VITE_KEYCLOAK_REALM ?? "mfacrud",
    clientId: env.VITE_KEYCLOAK_CLIENT_ID ?? "mfacrud-web",
  },
} as const;
