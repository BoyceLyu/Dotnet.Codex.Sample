using System.Text.Json;

namespace Dotnet.Codex.Sample.Tools;

internal sealed class ListMcpResourceTemplatesTool : IMcpToolProvider
{
    public McpTool Build() => new(
        "list_mcp_resource_templates",
        "列出 MCP 资源模板（提供 file://{path} 模板）",
        Schemas.Object(new()
        {
            { "server", Schemas.String("可选服务器名") },
            { "cursor", Schemas.String("分页游标") }
        }),
        _ => Task.FromResult(ToolHelpers.Json(new
        {
            templates = new[]
            {
                new
                {
                    uri_template = "file://{path}",
                    name = "local-file",
                    description = "Read a local file by absolute path"
                }
            },
            next_cursor = (string?)null
        }))
    );
}
