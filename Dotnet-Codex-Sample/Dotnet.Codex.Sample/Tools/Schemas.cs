using System.Text.Json;

namespace Dotnet.Codex.Sample.Tools;

internal static class Schemas
{
    public static JsonElement Boolean(string? description) => Serialize(new
    {
        type = "boolean",
        description
    });

    public static JsonElement String(string? description) => Serialize(new
    {
        type = "string",
        description
    });

    public static JsonElement Number(string? description) => Serialize(new
    {
        type = "number",
        description
    });

    public static JsonElement Array(JsonElement items, string? description = null) => Serialize(new
    {
        type = "array",
        items,
        description
    });

    public static JsonElement Object(Dictionary<string, JsonElement> properties,
        string? description = null,
        IEnumerable<string>? required = null,
        bool additionalProperties = false) => Serialize(new
    {
        type = "object",
        description,
        properties = properties ?? new Dictionary<string, JsonElement>(),
        required,
        additionalProperties
    });

    private static JsonElement Serialize(object obj) => JsonSerializer.SerializeToElement(obj);
}
