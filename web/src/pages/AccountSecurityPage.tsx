import { useEffect, useState } from "react";
import { apiFetch } from "../api/client";
import type { TwoFactorStatus } from "../api/types";
import { useAuth } from "../auth/AuthContext";

export function AccountSecurityPage() {
  const auth = useAuth();
  const [reload, setReload] = useState(0);
  const [status, setStatus] = useState<TwoFactorStatus | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;

    void (async () => {
      try {
        const current = await apiFetch<TwoFactorStatus>("/auth/2fa/status");
        if (active) {
          setStatus(current);
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
  }, [reload]);

  async function remove(credentialId: string) {
    if (!window.confirm("Remover este autenticador? Você voltará a entrar apenas com a senha.")) {
      return;
    }

    try {
      await apiFetch(`/auth/2fa/${credentialId}`, { method: "DELETE" });
      setReload((current) => current + 1);
    } catch (problem) {
      setError(problem instanceof Error ? problem.message : "Falha ao remover.");
    }
  }

  return (
    <section>
      <h1>Segurança da conta</h1>

      {error && <p className="error">{error}</p>}

      <div className="card">
        <h2>Verificação em duas etapas</h2>

        {status?.enabled ? (
          <>
            <p>Ativada. Ao entrar, o Keycloak pede o código de 6 dígitos do aplicativo, que muda a cada 30 segundos.</p>
            <ul className="devices">
              {status.devices.map((device) => (
                <li key={device.id}>
                  <span>
                    <strong>{device.label ?? "Autenticador"}</strong>
                    <span className="muted"> · adicionado em {new Date(device.createdAt).toLocaleString("pt-BR")}</span>
                  </span>
                  <button type="button" className="link danger" onClick={() => void remove(device.id)}>
                    Remover
                  </button>
                </li>
              ))}
            </ul>
          </>
        ) : (
          <>
            <p>
              Desativada. Ao ativar, o Keycloak mostra um QR code para escanear no aplicativo autenticador do celular
              (2FAS, Google Authenticator, Authy) e confirma com o primeiro código.
            </p>
            <button type="button" onClick={auth.configureTotp}>
              Ativar 2FA
            </button>
          </>
        )}
      </div>

      <p className="muted">
        Senha, e-mail e dispositivos também podem ser gerenciados na{" "}
        <a href={auth.accountUrl()} target="_blank" rel="noreferrer">
          conta do Keycloak
        </a>
        .
      </p>
    </section>
  );
}
