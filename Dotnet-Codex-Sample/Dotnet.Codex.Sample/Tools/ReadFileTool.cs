using System.Text.Json;

namespace Dotnet.Codex.Sample.Tools;

internal sealed class ReadFileTool : IMcpToolProvider
{
    public McpTool Build() => new(
        "read_file",
        "读取文件（真实 IO，返回行切片 JSON）",
        Schemas.Object(new()
        {
            { "file_path", Schemas.String("绝对路径") },
            { "offset", Schemas.Number("起始行，1-based") },
            { "limit", Schemas.Number("行数上限") },
            { "mode", Schemas.String("模式 slice/indentation") }
        }, required: new[] { "file_path" }),
        async args =>
        {
            if (args is null || !args.Value.TryGetProperty("file_path", out var fp))
            {
                return "缺少 file_path";
            }

            var path = fp.GetString() ?? string.Empty;
            if (!File.Exists(path))
            {
                return $"文件不存在: {path}";
            }

            var offset = args.Value.TryGetProperty("offset", out var off) ? Math.Max(1, off.GetInt32()) : 1;
            var limit = args.Value.TryGetProperty("limit", out var lim) ? Math.Max(1, lim.GetInt32()) : 200;

            var lines = await File.ReadAllLinesAsync(path);
            var startIndex = Math.Max(0, offset - 1);
            var slice = lines.Skip(startIndex).Take(limit)
                .Select((text, idx) => new { line = offset + idx, text })
                .ToArray();

            return ToolHelpers.Json(new { file_path = path, offset, limit, lines = slice });
        }
    );
}
