using System.Text.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace EvolutionApi.API.Filters;

/// <summary>
/// OpenAPI schema transformer that replaces <see cref="JsonElement"/> with an open
/// JSON object schema (<c>{}</c>), resolving the crash caused by the ASP.NET Core
/// OpenAPI schema exporter being unable to introspect <see cref="JsonElement"/> —
/// since it is itself a raw JSON container, not a concrete serializable type.
/// </summary>
internal sealed class JsonElementSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type == typeof(JsonElement) ||
            context.JsonTypeInfo.Type == typeof(JsonElement?))
        {
            // Replace with an open object schema — accepts any valid JSON payload.
            schema.Type = JsonSchemaType.Object;
            schema.Properties?.Clear();
            schema.AdditionalPropertiesAllowed = true;
            schema.AdditionalProperties = null;
        }

        return Task.CompletedTask;
    }
}
