using System.Text.Json;

namespace Dotnet.Codex.Sample.Tools;

internal sealed class ExecCommandTool : IMcpToolProvider
{
    public McpTool Build() => new(
        "exec_command",
        "统一会话执行命令（持久进程，返回 session_id）",
        Schemas.Object(new()
        {
            { "cmd", Schemas.String("命令文本") },
            { "workdir", Schemas.String("工作目录") },
            { "shell", Schemas.String("shell 类型") },
            { "login", Schemas.Boolean("登录 shell") },
            { "yield_time_ms", Schemas.Number("等待输出毫秒") },
            { "max_output_tokens", Schemas.Number("最大 tokens") },
            { "sandbox_permissions", Schemas.String("沙箱权限") },
            { "justification", Schemas.String("提升权限理由") }
        }, required: new[] { "cmd" }),
        args =>
        {
            if (args is null || !args.Value.TryGetProperty("cmd", out var cmdEl))
            {
                return Task.FromResult("缺少 cmd");
            }

            var cmd = cmdEl.GetString() ?? string.Empty;
            var workdir = args.Value.TryGetProperty("workdir", out var wd) ? wd.GetString() : null;
            var shell = args.Value.TryGetProperty("shell", out var sh) ? sh.GetString() : null;
            var login = args.Value.TryGetProperty("login", out var lg) && lg.GetBoolean();

            var sessionId = ExecSessionManager.Start(cmd, workdir, shell, login);
            return Task.FromResult(ToolHelpers.Json(new { session_id = sessionId }));
        }
    );
}
