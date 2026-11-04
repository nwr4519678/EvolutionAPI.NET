namespace EvolutionApi.Dashboard.Server.Endpoints;

public static class ApiProxyEndpoints
{
    private static readonly string[] Methods = ["GET", "POST", "PUT", "PATCH", "DELETE"];

    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapMethods("/manager/api/{**path}", Methods, Forward)
            .RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> Forward(
        HttpContext context,
        IHttpClientFactory clients,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var serverApiKey = configuration["Evolution:ApiKey"];
        if (string.IsNullOrWhiteSpace(serverApiKey))
            return Results.Problem("The API key is not configured on the dashboard server.", statusCode: StatusCodes.Status503ServiceUnavailable);

        var path = context.Request.RouteValues.GetValueOrDefault("path")?.ToString() ?? string.Empty;
        var apiBase = new Uri(configuration["ApiBaseUrl"] ?? "http://localhost:5051/", UriKind.Absolute);
        var target = new Uri(apiBase, path + context.Request.QueryString);
        using var outgoing = new HttpRequestMessage(new HttpMethod(context.Request.Method), target);
        outgoing.Headers.TryAddWithoutValidation("apikey", serverApiKey);

        if (context.Request.ContentLength is > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
        {
            outgoing.Content = new StreamContent(context.Request.Body);
            if (!string.IsNullOrWhiteSpace(context.Request.ContentType))
                outgoing.Content.Headers.TryAddWithoutValidation("Content-Type", context.Request.ContentType);
        }

        using var response = await clients.CreateClient("evolution-api").SendAsync(
            outgoing, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        context.Response.StatusCode = (int)response.StatusCode;
        if (response.Content.Headers.ContentType is { } contentType)
            context.Response.ContentType = contentType.ToString();
        if (response.Content.Headers.ContentLength is { } length)
            context.Response.ContentLength = length;
        await response.Content.CopyToAsync(context.Response.Body, cancellationToken);
        return Results.Empty;
    }
}
