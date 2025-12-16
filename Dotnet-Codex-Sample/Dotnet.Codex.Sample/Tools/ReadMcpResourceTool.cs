using System.Text.Json;

namespace Dotnet.Codex.Sample.Tools;

internal sealed class ReadMcpResourceTool : IMcpToolProvider
{
    public McpTool Build() => new(
        "read_mcp_resource",
        "读取 MCP 资源（支持 file:// 与 http/https，返回 JSON 内容）",
        Schemas.Object(new()
        {
            { "server", Schemas.String("服务器名") },
            { "uri", Schemas.String("资源 URI") }
        }, required: new[] { "server", "uri" }),
        async args =>
        {
            if (args is null || !args.Value.TryGetProperty("uri", out var uriEl))
            {
                return "缺少 uri";
            }

            var uriText = uriEl.GetString() ?? string.Empty;
            string content;

            if (uriText.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                var path = uriText.Substring("file://".Length);
                if (!File.Exists(path))
                {
                    return $"文件不存在: {path}";
                }
                content = await File.ReadAllTextAsync(path);
            }

            else if (Uri.TryCreate(uriText, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                using var client = new HttpClient();
                content = await client.GetStringAsync(uri);
            }

            else
            {
                return $"不支持的 URI: {uriText}";
            }

            return ToolHelpers.Json(new { uri = uriText, content });
        }
    );
}
