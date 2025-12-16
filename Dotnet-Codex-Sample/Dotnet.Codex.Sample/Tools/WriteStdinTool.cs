using System.Text.Json;

namespace Dotnet.Codex.Sample.Tools;

internal sealed class WriteStdinTool : IMcpToolProvider
{
    public McpTool Build() => new(
        "write_stdin",
        "向统一会话写入 stdin（真实持久进程）",
        Schemas.Object(new()
        {
            { "session_id", Schemas.Number("会话 id") },
            { "chars", Schemas.String("写入内容") },
            { "yield_time_ms", Schemas.Number("等待输出毫秒") },
            { "max_output_tokens", Schemas.Number("最大 tokens") }
        }, required: new[] { "session_id" }),
        async args =>
        {
            if (args is null || !args.Value.TryGetProperty("session_id", out var idEl))
            {
                return "缺少 session_id";
            }

            var sessionId = idEl.GetInt32();
            var input = args.Value.TryGetProperty("chars", out var ch) ? ch.GetString() : null;
            var yieldMs = args.Value.TryGetProperty("yield_time_ms", out var y) ? y.GetInt32() : 100;

            var output = await ExecSessionManager.WriteAsync(sessionId, input, yieldMs);
            return ToolHelpers.Json(new { output });
        }
    );
}
