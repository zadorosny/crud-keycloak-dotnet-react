import { useEffect, useState, type ReactNode } from "react";
import { AuthContext, type AuthValue } from "./AuthContext";
import { initKeycloak, keycloak, rolesFromToken } from "./keycloak";

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState({ ready: false, authenticated: false });

  useEffect(() => {
    let active = true;

    initKeycloak()
      .then((authenticated) => {
        if (active) {
          setState({ ready: true, authenticated });
        }
      })
      .catch(() => {
        if (active) {
          setState({ ready: true, authenticated: false });
        }
      });

    return () => {
      active = false;
    };
  }, []);

  const roles = state.authenticated ? rolesFromToken() : [];
  const redirectUri = window.location.origin;

  const value: AuthValue = {
    ready: state.ready,
    authenticated: state.authenticated,
    username: (keycloak.tokenParsed?.preferred_username as string | undefined) ?? null,
    roles,
    hasRole: (...wanted) => wanted.some((role) => roles.includes(role)),
    login: () => void keycloak.login(),
    register: () => void keycloak.register(),
    logout: () => void keycloak.logout({ redirectUri }),
    configureTotp: () => void keycloak.login({ action: "CONFIGURE_TOTP" }),
    accountUrl: () => keycloak.createAccountUrl(),
  };

  return <AuthContext value={value}>{children}</AuthContext>;
}
