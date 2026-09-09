namespace MfaCrud.Api.Common;

public static class SecurityHeaders
{
    /// <summary>
    /// Cabeçalhos básicos para uma API JSON. Não há HTML servido aqui, então nada de CSP:
    /// o que importa é não deixar o browser adivinhar tipo, enquadrar a resposta ou vazar a URL.
    /// As respostas de /auth/* falam da conta de quem chamou e não podem ficar em cache.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";

            if (context.Request.Path.StartsWithSegments("/api/v1/auth", StringComparison.OrdinalIgnoreCase))
            {
                headers.CacheControl = "no-store";
                headers.Pragma = "no-cache";
            }

            await next();
        });
}
