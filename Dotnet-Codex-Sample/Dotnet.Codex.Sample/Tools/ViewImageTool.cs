using System.Text.Json;

namespace Dotnet.Codex.Sample.Tools;

internal sealed class ViewImageTool : IMcpToolProvider
{
    public McpTool Build() => new(
        "view_image",
        "关联本地图片（读取大小并输出 base64 预览）",
        Schemas.Object(new()
        {
            { "path", Schemas.String("图片路径") }
        }, required: new[] { "path" }),
        async args =>
        {
            if (args is null || !args.Value.TryGetProperty("path", out var p))
            {
                return "缺少 path";
            }

            var path = p.GetString() ?? string.Empty;
            if (!File.Exists(path))
            {
                return $"文件不存在: {path}";
            }

            var bytes = await File.ReadAllBytesAsync(path);
            var base64 = Convert.ToBase64String(bytes.Take(2_000_000).ToArray()); // cap preview
            return $"size={bytes.Length} bytes\nbase64_preview={base64}";
        }
    );
}
