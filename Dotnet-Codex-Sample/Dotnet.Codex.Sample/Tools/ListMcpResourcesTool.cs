using System.Text.Json;

namespace Dotnet.Codex.Sample.Tools;

internal sealed class ListMcpResourcesTool : IMcpToolProvider
{
    public McpTool Build() => new(
        "list_mcp_resources",
        "列出 MCP 资源（扫描目录生成 file:// 资源列表）",
        Schemas.Object(new()
        {
            { "server", Schemas.String("可选服务器名") },
            { "cursor", Schemas.String("分页游标") }
        }),
        _ =>
        {
            var root = Directory.GetCurrentDirectory();
            var resources = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Take(50)
                .Select(f => new { uri = $"file://{f}", name = Path.GetFileName(f) })
                .ToArray();

            return Task.FromResult(ToolHelpers.Json(new { resources, next_cursor = (string?)null }));
        }
    );
}
