namespace TrainingManagement.Web.Infrastructure;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public const string ContentSecurityPolicy =
        "default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'self'; " +
        "form-action 'self'; img-src 'self' data: https:; font-src 'self' data:; " +
        "style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "frame-src 'self' https://www.youtube.com https://www.youtube-nocookie.com https://player.vimeo.com https://app.anam.ai; " +
        "media-src 'self' blob: https:; connect-src 'self' https://api.anam.ai wss://*.anam.ai;";

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(self), microphone=(self), geolocation=()";
        headers["X-Frame-Options"] = "SAMEORIGIN";
        headers["Content-Security-Policy"] = ContentSecurityPolicy;
        await next(context);
    }
}
