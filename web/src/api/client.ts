import { config } from "../config";
import { keycloak } from "../auth/keycloak";

export class ApiError extends Error {
  readonly status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

/**
 * Renova o token quando faltam menos de 30s e injeta o Bearer. Um 401 significa que a sessão
 * acabou: manda para a tela de login do Keycloak.
 */
export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  if (keycloak.authenticated) {
    try {
      await keycloak.updateToken(30);
    } catch {
      await keycloak.login();
      throw new ApiError(401, "Sessão expirada.");
    }
  }

  const headers = new Headers(init.headers);
  if (keycloak.token) {
    headers.set("Authorization", `Bearer ${keycloak.token}`);
  }
  if (init.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(`${config.apiUrl}${path}`, { ...init, headers });

  if (response.status === 401) {
    await keycloak.login();
    throw new ApiError(401, "Sessão expirada.");
  }

  if (!response.ok) {
    throw new ApiError(response.status, await readError(response));
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

async function readError(response: Response): Promise<string> {
  try {
    const problem = (await response.json()) as { title?: string; detail?: string; errors?: Record<string, string[]> };
    const validation = problem.errors && Object.values(problem.errors).flat().join(" ");
    return validation || problem.detail || problem.title || `Erro ${response.status}.`;
  } catch {
    return `Erro ${response.status}.`;
  }
}
