using System.Text.Json;

namespace Dotnet.Codex.Sample.Tools;

internal sealed class ContainerExecTool : IMcpToolProvider
{
    public McpTool Build() => new(
        "container.exec",
        "容器执行别名（使用 cmd /c 执行）",
        Schemas.Object(new()
        {
            { "command", Schemas.Array(Schemas.String(null), "命令数组") },
            { "workdir", Schemas.String("工作目录") },
            { "timeout_ms", Schemas.Number("超时毫秒") }
        }, required: new[] { "command" }),
        async args =>
        {
            if (args is null || !args.Value.TryGetProperty("command", out var cmdArr) || cmdArr.ValueKind != JsonValueKind.Array)
            {
                return "缺少 command 数组";
            }

            var parts = cmdArr.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToArray();
            if (parts.Length == 0)
            {
                return "命令为空";
            }

            var joined = string.Join(" ", parts);
            var workdir = args.Value.TryGetProperty("workdir", out var wd) ? wd.GetString() : null;
            var timeout = args.Value.TryGetProperty("timeout_ms", out var t) ? t.GetInt32() : (int?)null;

            var (exit, stdout, stderr) = await ProcessRunner.RunAsync("cmd.exe", $"/c {joined}", workdir, timeout);
            return ToolHelpers.FormatProcessResult(exit, stdout, stderr);
        }
    );
}
