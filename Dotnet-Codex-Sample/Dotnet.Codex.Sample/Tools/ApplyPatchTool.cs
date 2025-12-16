using System.Text.Json;

namespace Dotnet.Codex.Sample.Tools;

internal sealed class ApplyPatchTool : IMcpToolProvider
{
    public McpTool Build() => new(
        "apply_patch",
        "应用补丁（调用 git apply，如不可用则报错）",
        Schemas.Object(new()
        {
            { "input", Schemas.String("补丁文本") }
        }, required: new[] { "input" }),
        async args =>
        {
            if (args is null || !args.Value.TryGetProperty("input", out var patchElement))
            {
                return "缺少 input 补丁文本";
            }

            var patchText = patchElement.GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(patchText))
            {
                return "空补丁";
            }

            var temp = Path.GetTempFileName();
            await File.WriteAllTextAsync(temp, patchText, Encoding.UTF8);

            // 优先使用 git apply，若不可用再尝试 patch
            var (exit, stdout, stderr) = await ProcessRunner.RunAsync(
                "git",
                $"apply --unsafe-paths \"{temp}\"",
                workdir: Directory.GetCurrentDirectory());

            if (exit != 0)
            {
                (exit, stdout, stderr) = await ProcessRunner.RunAsync(
                    "patch",
                    $"-p0 -i \"{temp}\"",
                    workdir: Directory.GetCurrentDirectory());
            }

            File.Delete(temp);

            if (exit == 0)
            {
                return string.IsNullOrWhiteSpace(stdout) ? "补丁应用成功" : stdout.Trim();
            }

            return $"补丁失败 (exit {exit})\n{stderr}";
        }
    );
}
