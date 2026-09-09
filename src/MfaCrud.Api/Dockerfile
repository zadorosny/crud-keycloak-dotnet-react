# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Restore separado para aproveitar o cache de camadas quando só o código muda.
COPY src/MfaCrud.Api/MfaCrud.Api.csproj src/MfaCrud.Api/
RUN dotnet restore src/MfaCrud.Api/MfaCrud.Api.csproj

COPY src/ src/
RUN dotnet publish src/MfaCrud.Api/MfaCrud.Api.csproj --configuration Release --no-restore -o /app

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .

# Sem HTTPS no container: quem termina TLS é o que estiver na frente.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# A imagem já traz o usuário "app" sem privilégios.
USER app
ENTRYPOINT ["dotnet", "MfaCrud.Api.dll"]
