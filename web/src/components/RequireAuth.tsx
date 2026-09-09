import type { ReactNode } from "react";
import { useAuth } from "../auth/AuthContext";

/**
 * Gate de renderização apenas: quem decide de verdade é a API, que valida o token e a role em
 * toda requisição.
 */
export function RequireAuth({ roles, children }: { roles?: string[]; children: ReactNode }) {
  const auth = useAuth();

  if (!auth.authenticated) {
    return (
      <section className="empty">
        <p>Entre para ver esta página.</p>
        <button type="button" onClick={auth.login}>
          Entrar
        </button>
      </section>
    );
  }

  if (roles && !auth.hasRole(...roles)) {
    return (
      <section className="empty">
        <p>Sua conta não tem permissão para esta página.</p>
      </section>
    );
  }

  return <>{children}</>;
}
