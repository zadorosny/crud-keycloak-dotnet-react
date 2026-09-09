import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router";
import { apiFetch } from "../api/client";
import type { Product, ProductRequest } from "../api/types";

const empty: ProductRequest = { name: "", description: "", price: 0, stock: 0 };

export function ProductFormPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [form, setForm] = useState<ProductRequest>(empty);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!id) {
      return;
    }

    apiFetch<Product>(`/products/${id}`)
      .then((product) =>
        setForm({
          name: product.name,
          description: product.description ?? "",
          price: product.price,
          stock: product.stock,
        }),
      )
      .catch((problem: unknown) => setError(problem instanceof Error ? problem.message : "Falha ao carregar."));
  }, [id]);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);

    try {
      const body = JSON.stringify({ ...form, description: form.description || null });
      await (id
        ? apiFetch<Product>(`/products/${id}`, { method: "PUT", body })
        : apiFetch<Product>("/products", { method: "POST", body }));
      void navigate("/products");
    } catch (problem) {
      setError(problem instanceof Error ? problem.message : "Falha ao salvar.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <section>
      <h1>{id ? "Editar produto" : "Novo produto"}</h1>

      {error && <p className="error">{error}</p>}

      <form className="form" onSubmit={(event) => void submit(event)}>
        <label>
          Nome
          <input
            required
            maxLength={120}
            value={form.name}
            onChange={(event) => setForm({ ...form, name: event.target.value })}
          />
        </label>

        <label>
          Descrição
          <textarea
            rows={3}
            maxLength={1000}
            value={form.description ?? ""}
            onChange={(event) => setForm({ ...form, description: event.target.value })}
          />
        </label>

        <div className="pair">
          <label>
            Preço
            <input
              required
              type="number"
              min={0}
              step="0.01"
              value={form.price}
              onChange={(event) => setForm({ ...form, price: Number(event.target.value) })}
            />
          </label>

          <label>
            Estoque
            <input
              required
              type="number"
              min={0}
              step="1"
              value={form.stock}
              onChange={(event) => setForm({ ...form, stock: Number(event.target.value) })}
            />
          </label>
        </div>

        <div className="actions">
          <button type="submit" disabled={busy}>
            {busy ? "Salvando…" : "Salvar"}
          </button>
          <button type="button" className="secondary" onClick={() => void navigate("/products")}>
            Cancelar
          </button>
        </div>
      </form>
    </section>
  );
}
