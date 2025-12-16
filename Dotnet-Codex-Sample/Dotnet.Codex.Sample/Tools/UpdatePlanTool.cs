using System.Text.Json;

namespace Dotnet.Codex.Sample.Tools;

internal sealed class UpdatePlanTool : IMcpToolProvider
{
    public McpTool Build() => new(
        "update_plan",
        "更新计划（记录摘要）",
        Schemas.Object(new()
        {
            { "summary", Schemas.String("计划摘要") },
            { "next_steps", Schemas.Array(Schemas.String("下一步"), "后续步骤列表") }
        }),
        async args =>
        {
            var text = ToolHelpers.RenderArgs("已接收计划更新", args);
            var logPath = Path.Combine(Directory.GetCurrentDirectory(), "plan_updates.log");
            await File.AppendAllTextAsync(logPath, text + Environment.NewLine);
            return text;
        }
    );
}
