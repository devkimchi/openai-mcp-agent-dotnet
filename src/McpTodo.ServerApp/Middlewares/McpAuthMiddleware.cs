using System.Net;

namespace McpTodoList.ContainerApp.Middlewares;

public class McpAuthMiddleware(RequestDelegate next, IConfiguration config)
{
    private const string ApiKeyHeaderName = "x-api-key";
    private const string ApiKeyQueryParameterName = "code";

    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly string _expectedApiKey = config["McpServer:ApiKey"] ?? string.Empty;

    public async Task InvokeAsync(HttpContext context)
    {
        // Bypass localhost requests
        var localhost = IsLocalhost(context.Connection);
        if (localhost == true)
        {
            await this._next(context);
            return;
        }

        if (context.Request.Method != HttpMethods.Get)
        {
            await this._next(context);
            return;
        }

        if (string.IsNullOrWhiteSpace(this._expectedApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("API key not set.");
            return;
        }

        var apiKey = GetApiKeyFromRequest(context.Request);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("API key not found.");
            return;
        }
        if (string.Equals(apiKey, this._expectedApiKey, StringComparison.InvariantCultureIgnoreCase) == false)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid API key.");
            return;
        }

        await this._next(context);
    }

    private static bool IsLocalhost(ConnectionInfo connection)
    {
        if (connection.RemoteIpAddress == null && connection.LocalIpAddress == null)
        {
            return true;
        }

        if (connection.RemoteIpAddress != null)
        {
            return connection.LocalIpAddress != null
                ? connection.RemoteIpAddress.Equals(connection.LocalIpAddress)
                : IPAddress.IsLoopback(connection.RemoteIpAddress);
        }

        return false;
    }

    private static string? GetApiKeyFromRequest(HttpRequest request)
    {
        if (request.Headers.TryGetValue(ApiKeyHeaderName, out var headerValue))
        {
            return headerValue.ToString();
        }

        if (request.Query.TryGetValue(ApiKeyQueryParameterName, out var queryValue))
        {
            return queryValue.ToString();
        }

        return null;
    }
}

public static class McpAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseMcpAuth(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        
        app.UseMiddleware<McpAuthMiddleware>();

        return app;
    }
}
