import Keycloak from "keycloak-js";
import { config } from "../config";

// Uma instância única, fora do ciclo de render: no StrictMode o React monta o componente duas
// vezes em desenvolvimento e o adapter não pode ser inicializado duas vezes.
export const keycloak = new Keycloak(config.keycloak);

let initialization: Promise<boolean> | null = null;

export function initKeycloak(): Promise<boolean> {
  initialization ??= keycloak.init({ onLoad: "check-sso", pkceMethod: "S256" });
  return initialization;
}

/** Roles do realm, lidas do claim `roles` do token. Só decidem o que renderizar. */
export function rolesFromToken(): string[] {
  const parsed = keycloak.tokenParsed as { roles?: string[] } | undefined;
  return parsed?.roles ?? [];
}
