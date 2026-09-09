using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MfaCrud.Api.Keycloak;

/// <summary>Turns an Admin API failure into a 502 without leaking what Keycloak said.</summary>
public sealed class KeycloakAdminExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not KeycloakAdminException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status502BadGateway;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status502BadGateway,
                Title = "Identity provider unavailable",
                Detail = "The request could not be completed because Keycloak did not answer as expected.",
            },
        });
    }
}
