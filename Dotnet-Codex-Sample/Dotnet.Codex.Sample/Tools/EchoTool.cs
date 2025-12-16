using System.Text.Json;

namespace Dotnet.Codex.Sample.Tools;

internal sealed class EchoTool : IMcpToolProvider
{
    public McpTool Build() => new(
        "echo",
        "回显 text",
        Schemas.Object(new()
        {
            { "text", Schemas.String("要回显的文本") }
        }, required: new[] { "text" }),
        args =>
        {
            if (args is not null && args.Value.TryGetProperty("text", out var text))
            {
                return Task.FromResult(text.GetString() ?? string.Empty);
            }
            return Task.FromResult("缺少 text 参数");
        }
    );
}
