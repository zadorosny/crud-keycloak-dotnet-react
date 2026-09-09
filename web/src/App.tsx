import { Route, Routes } from "react-router";
import { Layout } from "./components/Layout";
import { RequireAuth } from "./components/RequireAuth";
import { useAuth } from "./auth/AuthContext";
import { AccountSecurityPage } from "./pages/AccountSecurityPage";
import { HomePage } from "./pages/HomePage";
import { ProductFormPage } from "./pages/ProductFormPage";
import { ProductsPage } from "./pages/ProductsPage";
import { UsersPage } from "./pages/UsersPage";

export function App() {
  const auth = useAuth();

  // Enquanto o adapter não terminou o check-sso não dá para saber se há sessão.
  if (!auth.ready) {
    return <p className="loading">Carregando…</p>;
  }

  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<HomePage />} />
        <Route
          path="products"
          element={
            <RequireAuth>
              <ProductsPage />
            </RequireAuth>
          }
        />
        <Route
          path="products/new"
          element={
            <RequireAuth roles={["admin", "staff"]}>
              <ProductFormPage />
            </RequireAuth>
          }
        />
        <Route
          path="products/:id/edit"
          element={
            <RequireAuth roles={["admin", "staff"]}>
              <ProductFormPage />
            </RequireAuth>
          }
        />
        <Route
          path="users"
          element={
            <RequireAuth roles={["admin"]}>
              <UsersPage />
            </RequireAuth>
          }
        />
        <Route
          path="account/security"
          element={
            <RequireAuth>
              <AccountSecurityPage />
            </RequireAuth>
          }
        />
        <Route path="*" element={<p className="empty">Página não encontrada.</p>} />
      </Route>
    </Routes>
  );
}
