-- Executado apenas na primeira inicialização do volume (docker-entrypoint-initdb.d).
-- Dois bancos no mesmo servidor: `mfacrud` (API) e `keycloak` (Keycloak).
CREATE DATABASE mfacrud;
CREATE DATABASE keycloak;
