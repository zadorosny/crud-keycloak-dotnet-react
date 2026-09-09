import { createContext, use } from "react";

export type AuthValue = {
  ready: boolean;
  authenticated: boolean;
  username: string | null;
  roles: string[];
  hasRole: (...roles: string[]) => boolean;
  login: () => void;
  register: () => void;
  logout: () => void;
  /** Abre a página do Keycloak que mostra o QR code do autenticador. */
  configureTotp: () => void;
  accountUrl: () => string;
};

export const AuthContext = createContext<AuthValue | null>(null);

export function useAuth(): AuthValue {
  const value = use(AuthContext);
  if (!value) {
    throw new Error("useAuth precisa estar dentro de <AuthProvider>.");
  }
  return value;
}
