import { useEffect, useState } from "react";
import { apiFetch } from "../api/client";
import type { User, UserPage } from "../api/types";

const ManagedRoles = ["admin", "staff", "customer"] as const;
const PageSize = 20;

export function UsersPage() {
  const [term, setTerm] = useState("");
  const [query, setQuery] = useState({ search: "", reload: 0 });
  const [page, setPage] = useState<UserPage | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState<string | null>(null);

  useEffect(() => {
    let active = true;

    void (async () => {
      try {
        const parameters = new URLSearchParams({ first: "0", max: String(PageSize) });
        if (query.search) {
          parameters.set("search", query.search);
        }

        const users = await apiFetch<UserPage>(`/users?${parameters}`);
        if (active) {
          setPage(users);
          setError(null);
        }
      } catch (problem) {
        if (active) {
          setError(problem instanceof Error ? problem.message : "Falha ao carregar.");
        }
      }
    })();

    return () => {
      active = false;
    };
  }, [query]);

  async function setRole(user: User, role: string) {
    setSaving(user.id);
    try {
      await apiFetch<User>(`/users/${user.id}/roles`, { method: "PUT", body: JSON.stringify({ roles: [role] }) });
      setQuery((current) => ({ ...current, reload: current.reload + 1 }));
    } catch (problem) {
      setError(problem instanceof Error ? problem.message : "Falha ao trocar o papel.");
    } finally {
      setSaving(null);
    }
  }

  return (
    <section>
      <div className="page-header">
        <h1>Usuários</h1>
      </div>

      <form
        className="toolbar"
        onSubmit={(event) => {
          event.preventDefault();
          setQuery((current) => ({ ...current, search: term.trim() }));
        }}
      >
        <input
          type="search"
          value={term}
          placeholder="Buscar por nome ou e-mail"
          onChange={(event) => setTerm(event.target.value)}
        />
        <button type="submit">Buscar</button>
      </form>

      {error && <p className="error">{error}</p>}

      <table>
        <thead>
          <tr>
            <th>Usuário</th>
            <th>Nome</th>
            <th>2FA</th>
            <th>Papel</th>
          </tr>
        </thead>
        <tbody>
          {page?.items.map((user) => {
            const current = ManagedRoles.find((role) => user.roles.includes(role)) ?? "";
            return (
              <tr key={user.id}>
                <td>{user.username}</td>
                <td className="muted">{[user.firstName, user.lastName].filter(Boolean).join(" ")}</td>
                <td>{user.twoFactorEnabled ? "ativo" : "—"}</td>
                <td>
                  <select
                    value={current}
                    disabled={saving === user.id}
                    onChange={(event) => void setRole(user, event.target.value)}
                  >
                    {current === "" && <option value="">sem papel</option>}
                    {ManagedRoles.map((role) => (
                      <option key={role} value={role}>
                        {role}
                      </option>
                    ))}
                  </select>
                </td>
              </tr>
            );
          })}
          {page?.items.length === 0 && (
            <tr>
              <td colSpan={4} className="muted">
                Nenhum usuário encontrado.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </section>
  );
}
