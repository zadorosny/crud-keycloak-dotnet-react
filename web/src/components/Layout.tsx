import { NavLink, Outlet } from "react-router";
import { useAuth } from "../auth/AuthContext";

export function Layout() {
  const auth = useAuth();

  return (
    <div className="app">
      <header>
        <nav>
          <NavLink to="/" end>
            MfaCrud
          </NavLink>
          {auth.authenticated && <NavLink to="/products">Produtos</NavLink>}
          {auth.hasRole("admin") && <NavLink to="/users">Usuários</NavLink>}
          {auth.authenticated && <NavLink to="/account/security">Segurança</NavLink>}
        </nav>

        <div className="session">
          {auth.authenticated ? (
            <>
              <span className="who">
                {auth.username}
                <span className="roles">{auth.roles.filter((r) => r !== "offline_access" && r !== "uma_authorization").join(" · ")}</span>
              </span>
              <button type="button" onClick={auth.logout}>
                Sair
              </button>
            </>
          ) : (
            <>
              <button type="button" onClick={auth.login}>
                Entrar
              </button>
              <button type="button" className="secondary" onClick={auth.register}>
                Criar conta
              </button>
            </>
          )}
        </div>
      </header>

      <main>
        <Outlet />
      </main>
    </div>
  );
}
