using System.Text.Json;

namespace Dotnet.Codex.Sample.Tools;

internal sealed class LocalShellTool : IMcpToolProvider
{
    public McpTool Build() => new(
        "local_shell",
        "本地 shell（与 Codex LocalShell 对齐，无参数，返回不可用提示）",
        Schemas.Object(new()),
        _ => Task.FromResult(ToolHelpers.Json(new { message = "local_shell 不提供命令执行（Windows 环境未实现）" }))
    );
}
