namespace MfaCrud.Api.Keycloak;

/// <summary>
/// Raised when the Keycloak Admin API is unreachable or answers with an error. The message is for
/// the log only: clients get a generic 502 (see <see cref="KeycloakAdminExceptionHandler"/>).
/// </summary>
public sealed class KeycloakAdminException(string message) : Exception(message);
