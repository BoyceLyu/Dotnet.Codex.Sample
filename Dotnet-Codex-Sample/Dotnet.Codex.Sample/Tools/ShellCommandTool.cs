using System.Text.Json;

namespace Dotnet.Codex.Sample.Tools;

internal sealed class ShellCommandTool : IMcpToolProvider
{
    public McpTool Build() => new(
        "shell_command",
        "执行 shell 字符串（cmd /c，返回结构化 stdout/stderr/exit_code）",
        Schemas.Object(new()
        {
            { "command", Schemas.String("命令字符串") },
            { "workdir", Schemas.String("工作目录") },
            { "login", Schemas.Boolean("登录 shell") },
            { "timeout_ms", Schemas.Number("超时毫秒") },
            { "sandbox_permissions", Schemas.String("沙箱权限") },
            { "justification", Schemas.String("提升权限理由") }
        }, required: new[] { "command" }),
        async args =>
        {
            if (args is null || !args.Value.TryGetProperty("command", out var cmd))
            {
                return "缺少 command";
            }

            var command = cmd.GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(command))
            {
                return "命令为空";
            }

            var workdir = args.Value.TryGetProperty("workdir", out var wd) ? wd.GetString() : null;
            var timeout = args.Value.TryGetProperty("timeout_ms", out var t) ? t.GetInt32() : (int?)null;

            var (exit, stdout, stderr) = await ProcessRunner.RunAsync("cmd.exe", $"/c {command}", workdir, timeout);
            return ToolHelpers.FormatProcessResult(exit, stdout, stderr);
        }
    );
}
