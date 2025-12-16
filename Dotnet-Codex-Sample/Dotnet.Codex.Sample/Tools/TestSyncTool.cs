using System.Text.Json;

namespace Dotnet.Codex.Sample.Tools;

internal sealed class TestSyncTool : IMcpToolProvider
{
    public McpTool Build() => new(
        "test_sync_tool",
        "同步屏障（本地 sleep 实现）",
        Schemas.Object(new()
        {
            { "sleep_before_ms", Schemas.Number("前置等待") },
            { "sleep_after_ms", Schemas.Number("后置等待") },
            { "barrier", Schemas.Object(new()
                {
                    { "id", Schemas.String("屏障 ID") },
                    { "participants", Schemas.Number("参与者数量") },
                    { "timeout_ms", Schemas.Number("超时") }
                }, required: new[] { "id", "participants" }) }
        }),
        async args =>
        {
            var before = args is not null && args.Value.TryGetProperty("sleep_before_ms", out var b) ? b.GetInt32() : 0;
            var after = args is not null && args.Value.TryGetProperty("sleep_after_ms", out var a) ? a.GetInt32() : 0;
            if (before > 0) await Task.Delay(before);
            // 简化：不实现跨进程屏障，仅回显参数
            if (after > 0) await Task.Delay(after);
            return ToolHelpers.RenderArgs("已完成同步等待", args);
        }
    );
}
