using System.Text.Json;

namespace CipherVault.Middleware
{
    /// <summary>
    /// Global exception middleware.
    /// - API paths (/api/*) get a JSON 500 body — never HTML, never stack traces.
    /// - MVC paths get re-executed to /Home/StatusCode?code=500.
    /// - Logs method + path only. NEVER query strings, bodies, headers, or values.
    /// </summary>
    public sealed class ExceptionHandlingMiddleware
    {
        private const string ErrorHandledKey = "__CipherVault_ExceptionHandled";

        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger
        )
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Guard against infinite recursion if the error page itself throws.
                if (context.Items.ContainsKey(ErrorHandledKey))
                {
                    throw;
                }
                context.Items[ErrorHandledKey] = true;

                // Log method + path only. Path.Value excludes the query string.
                // Never log ex.Message directly if it might echo user input.
                _logger.LogError(
                    ex,
                    "Unhandled exception. Method={Method} Path={Path}",
                    context.Request.Method,
                    context.Request.Path.Value
                );

                // If headers were already flushed, we cannot write an error response.
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning(
                        "Response already started; re-throwing to let the server terminate the connection."
                    );
                    throw;
                }

                // Reset response (Clear() resets status code to 200 — we set it back).
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                // -------- API branch: JSON, no HTML, no details --------
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.ContentType = "application/json; charset=utf-8";
                    var payload = JsonSerializer.Serialize(new { error = "internal_server_error" });
                    await context.Response.WriteAsync(payload);
                    return;
                }

                // -------- MVC branch: re-execute pipeline to render the 500 page --------
                context.Request.Path = "/Home/HttpError";
                context.Request.QueryString = new QueryString("?code=500");
                context.Request.Method = HttpMethods.Get;

                await _next(context);
            }
        }
    }
}
