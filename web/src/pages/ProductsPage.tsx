import { useEffect, useState } from "react";
import { Link } from "react-router";
import { apiFetch } from "../api/client";
import type { PagedResult, Product } from "../api/types";
import { useAuth } from "../auth/AuthContext";

const PageSize = 10;

export function ProductsPage() {
  const auth = useAuth();
  const [term, setTerm] = useState("");
  const [query, setQuery] = useState({ page: 1, search: "", reload: 0 });
  const [result, setResult] = useState<PagedResult<Product> | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;

    void (async () => {
      try {
        const parameters = new URLSearchParams({ page: String(query.page), pageSize: String(PageSize) });
        if (query.search) {
          parameters.set("search", query.search);
        }

        const page = await apiFetch<PagedResult<Product>>(`/products?${parameters}`);
        if (active) {
          setResult(page);
          setError(null);
        }
      } catch (problem) {
        if (active) {
          setError(problem instanceof Error ? problem.message : "Falha ao carregar.");
        }
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    })();

    return () => {
      active = false;
    };
  }, [query]);

  async function remove(product: Product) {
    if (!window.confirm(`Excluir "${product.name}"?`)) {
      return;
    }

    try {
      await apiFetch(`/products/${product.id}`, { method: "DELETE" });
      setQuery((current) => ({ ...current, reload: current.reload + 1 }));
    } catch (problem) {
      setError(problem instanceof Error ? problem.message : "Falha ao excluir.");
    }
  }

  const canWrite = auth.hasRole("admin", "staff");
  const pages = result ? Math.max(1, Math.ceil(result.total / result.pageSize)) : 1;

  return (
    <section>
      <div className="page-header">
        <h1>Produtos</h1>
        {canWrite && (
          <Link className="button" to="/products/new">
            Novo produto
          </Link>
        )}
      </div>

      <form
        className="toolbar"
        onSubmit={(event) => {
          event.preventDefault();
          setLoading(true);
          setQuery((current) => ({ ...current, page: 1, search: term.trim() }));
        }}
      >
        <input type="search" value={term} placeholder="Buscar por nome" onChange={(event) => setTerm(event.target.value)} />
        <button type="submit">Buscar</button>
      </form>

      {error && <p className="error">{error}</p>}

      <table>
        <thead>
          <tr>
            <th>Nome</th>
            <th>Descrição</th>
            <th className="right">Preço</th>
            <th className="right">Estoque</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {result?.items.map((product) => (
            <tr key={product.id}>
              <td>{product.name}</td>
              <td className="muted">{product.description}</td>
              <td className="right">{product.price.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}</td>
              <td className="right">{product.stock}</td>
              <td className="row-actions">
                {canWrite && <Link to={`/products/${product.id}/edit`}>Editar</Link>}
                {auth.hasRole("admin") && (
                  <button type="button" className="link danger" onClick={() => void remove(product)}>
                    Excluir
                  </button>
                )}
              </td>
            </tr>
          ))}
          {result?.items.length === 0 && (
            <tr>
              <td colSpan={5} className="muted">
                {loading ? "Carregando…" : "Nenhum produto encontrado."}
              </td>
            </tr>
          )}
        </tbody>
      </table>

      <div className="pager">
        <button
          type="button"
          disabled={query.page <= 1}
          onClick={() => setQuery((current) => ({ ...current, page: current.page - 1 }))}
        >
          Anterior
        </button>
        <span>
          Página {query.page} de {pages}
          {result && ` · ${result.total} no total`}
        </span>
        <button
          type="button"
          disabled={query.page >= pages}
          onClick={() => setQuery((current) => ({ ...current, page: current.page + 1 }))}
        >
          Próxima
        </button>
      </div>
    </section>
  );
}
