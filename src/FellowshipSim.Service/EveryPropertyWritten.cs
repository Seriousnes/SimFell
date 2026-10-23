using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace FellowshipSim.Service;

/// <summary>
/// Each property of an object schema is required, the object has no other properties, and a property is nullable only
/// where its type is. The JSON serializer writes every property, null ones included.
/// </summary>
internal sealed class EveryPropertyWritten : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Kind != JsonTypeInfoKind.Object || schema.Properties is not { Count: > 0 } properties) return Task.CompletedTask;

        schema.Required = new HashSet<string>(properties.Keys);
        schema.AdditionalPropertiesAllowed = false;
        // The document has a property with only a getter as nullable whatever its annotation. The getter's annotation is used instead.
        foreach (var property in context.JsonTypeInfo.Properties)
        {
            if (!property.IsGetNullable && properties.TryGetValue(property.Name, out var value) && value is OpenApiSchema { Type: { } type } described)
            {
                described.Type = type & ~JsonSchemaType.Null;
            }
        }
        return Task.CompletedTask;
    }
}
