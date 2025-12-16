using System.Text.Json;

namespace Dotnet.Codex.Sample.Tools;

internal interface IMcpToolProvider
{
    McpTool Build();
}
