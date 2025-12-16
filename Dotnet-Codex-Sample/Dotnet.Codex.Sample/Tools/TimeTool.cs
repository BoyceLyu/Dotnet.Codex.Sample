using System.Text.Json;

namespace Dotnet.Codex.Sample.Tools;

internal sealed class TimeTool : IMcpToolProvider
{
    public McpTool Build() => new(
        "time",
        "返回当前 UTC 时间",
        Schemas.Object(new()),
        _ => Task.FromResult(DateTime.UtcNow.ToString("O"))
    );
}
