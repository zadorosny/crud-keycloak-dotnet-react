# Realm `mfacrud`

Todo o Keycloak deste projeto está em [`realm-export.json`](realm-export.json). O arquivo é
importado na subida do container (`start-dev --import-realm`) e é a **única** fonte de verdade:
mudanças feitas no console admin precisam voltar para cá antes do commit.

Recriar do zero (é o teste de que o arquivo está completo):

```bash
docker compose down -v && docker compose up -d
```

## O que o arquivo configura

| Bloco | Conteúdo |
|---|---|
| Roles | `admin`, `staff`, `customer` + as built-in `offline_access` e `uma_authorization` |
| Default role | `default-roles-mfacrud` compõe `customer` — todo usuário registrado nasce customer |
| Clients | `mfacrud-web` (público, PKCE S256), `mfacrud-api` (só audience), `mfacrud-postman` (dev, direct grant), `mfacrud-admin-svc` (service account) |
| Mappers | audience `mfacrud-api` e realm roles no claim top-level `roles` (multivalued), em `web` e `postman` |
| Login | registro ON, e-mail como username, login por e-mail, verify e-mail OFF, forgot password OFF, remember me OFF |
| 2FA | OTP policy TOTP / HmacSHA1 / 6 dígitos / 30s / look-ahead 1; required action `CONFIGURE_TOTP` habilitada e **não** default |
| Brute force | ligado, estratégia `MULTIPLE`, 30 falhas, espera incremental até 15 min |
| Tokens | access token 15 min, SSO idle 30 min, SSO max 8 h |
| Usuários | 4 de teste + o service account, com roles e (num deles) credencial TOTP |

Os **authentication flows são os padrões do Keycloak** e por isso não aparecem no JSON. É de
propósito: o flow `browser` já traz o subflow `Browser - Conditional 2FA` (CONDITIONAL →
`conditional-user-configured` + `auth-otp-form` ALTERNATIVE) e o `direct grant` já traz
`Direct Grant - Conditional OTP`. É isso que torna o 2FA **opcional**: o código só é pedido de quem
configurou um autenticador. Declarar os 21 flows built-in no JSON só adicionaria ruído e risco de
divergir da versão da imagem.

## Usuários de teste (somente dev)

| Usuário | Senha | Role | TOTP |
|---|---|---|---|
| `admin@test.local` | `Admin123!` | `admin` | não |
| `staff@test.local` | `Staff123!` | `staff` | não |
| `customer@test.local` | `Customer123!` | `customer` | não |
| `customer2fa@test.local` | `Customer123!` | `customer` | **sim** (pré-configurado) |

A credencial TOTP do `customer2fa` usa o secret `PORTFOLIO2FASECRET20`. O Keycloak usa os **bytes
UTF-8** do valor como chave HMAC — o QR code mostra `Base32(bytes)`, mas o que vale para gerar o
código é o texto cru. Nos testes e no Postman:

```csharp
new Totp(Encoding.UTF8.GetBytes("PORTFOLIO2FASECRET20")).ComputeTotp()
```

Isso foi confirmado na prática: a página "Mobile Authenticator Setup" do Keycloak 26.7.3 traz o
secret cru no campo `totpSecret`, e o direct grant aceita o código gerado a partir dele.

## Segredos

O client secret do `mfacrud-admin-svc` **não** está no arquivo: o JSON tem o placeholder
`${MFACRUD_ADMIN_SVC_SECRET}`, que o Keycloak substitui na importação pela variável de ambiente de
mesmo nome (passada pelo `docker-compose.yml` a partir do `.env`). Nunca commitar o valor real.

## Exportar depois de mexer no console

```bash
docker compose exec keycloak /opt/keycloak/bin/kc.sh export \
  --file /tmp/realm.json --realm mfacrud --users realm_file
docker compose cp keycloak:/tmp/realm.json ./keycloak/realm-export.raw.json
```

O export é uma dump completo da versão instalada: ~2 mil linhas, com IDs gerados, todos os flows
built-in e **segredos em claro**. O `.gitignore` já barra `keycloak/*.raw.json` e
`keycloak/realm-export.*.json` justamente para o dump não ser commitado por engano. Antes de portar
qualquer coisa dele:

1. trocar o `secret` do `mfacrud-admin-svc` de volta pelo placeholder `${MFACRUD_ADMIN_SVC_SECRET}`;
2. conferir se o secret de algum outro client confidencial vazou para o arquivo;
3. **remover as credenciais OTP que não sejam a do `customer2fa`**: com `--users realm_file` o export
   leva junto o secret de todo autenticador cadastrado no ambiente local, inclusive os de celulares
   de verdade. Só o `test-authenticator` de secret `PORTFOLIO2FASECRET20` pertence ao repositório;
4. preferir portar **só o delta** para o `realm-export.json` versionado, mantendo-o enxuto e legível,
   em vez de substituir o arquivo inteiro pelo dump.

## Armadilhas já encontradas (Keycloak 26.7.3)

- **`roles.realm` desliga os built-in.** Ao declarar roles no JSON, o Keycloak não cria mais
  `offline_access` nem `uma_authorization` sozinho, e a importação falha com
  `Unable to find composite realm role: uma_authorization` se `default-roles-mfacrud` referenciar
  essas roles. Por isso as duas estão declaradas explicitamente.
- **`requiredActions` é tudo ou nada.** Declarar uma lista parcial faz o Keycloak registrar só
  aquelas — sumiriam `VERIFY_PROFILE`, `UPDATE_PASSWORD`, `delete_credential` (essa última é o que
  permite remover o autenticador pela Account Console). Por isso as 14 estão no arquivo.
- **Service account precisa de `fullScopeAllowed: true`.** Com `false`, as roles de
  `realm-management` ficam atribuídas ao usuário do service account mas são filtradas do token, e a
  Admin API responde `403`.
- **Usuários importados não recebem default roles.** A importação cria o usuário sem
  `default-roles-mfacrud`; as roles de cada um estão listadas explicitamente, incluindo as do client
  `account` (necessárias para a Account Console).
- **Direct grant sem `totp` responde `400 invalid_grant`**, não 401 — é o comportamento desta
  versão para `Invalid user credentials`. O que importa para o teste é que **nenhum token é
  emitido**.
