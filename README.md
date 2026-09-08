# MfaCrud

CRUD de produtos e usuários com roles (`admin`, `staff`, `customer`) e 2FA TOTP opcional, delegando toda a identidade ao **Keycloak**. A API .NET 10 é um resource server puro: só valida o token e aplica autorização por role.

## Como rodar (parcial, M0)

```bash
cp .env.example .env        # ajuste as senhas
docker compose up -d        # PostgreSQL 18 + Keycloak 26.7.3 (realm `mfacrud` importado)
dotnet build
```

- Keycloak: <http://localhost:8080> (admin do `.env`)
- Discovery: <http://localhost:8080/realms/mfacrud/.well-known/openid-configuration>
- API (M2+): <http://localhost:5080>
