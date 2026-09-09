import { Link } from "react-router";
import { useAuth } from "../auth/AuthContext";

export function HomePage() {
  const auth = useAuth();

  return (
    <section className="home">
      <h1>MfaCrud</h1>
      <p>
        CRUD de produtos e usuários com papéis <code>admin</code>, <code>staff</code> e <code>customer</code>. Toda a
        identidade fica no Keycloak: login, cadastro e o segundo fator por aplicativo autenticador.
      </p>

      {auth.authenticated ? (
        <div className="actions">
          <Link className="button" to="/products">
            Ver produtos
          </Link>
          <Link className="button secondary" to="/account/security">
            Segurança da conta
          </Link>
        </div>
      ) : (
        <div className="actions">
          <button type="button" onClick={auth.login}>
            Entrar
          </button>
          <button type="button" className="secondary" onClick={auth.register}>
            Criar conta
          </button>
        </div>
      )}
    </section>
  );
}
