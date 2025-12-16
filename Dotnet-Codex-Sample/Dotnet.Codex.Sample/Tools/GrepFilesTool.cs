using System.Text.Json;

namespace Dotnet.Codex.Sample.Tools;

internal sealed class GrepFilesTool : IMcpToolProvider
{
    public McpTool Build() => new(
        "grep_files",
        "文件搜索（真实 IO，正则匹配，返回匹配文件列表）",
        Schemas.Object(new()
        {
            { "pattern", Schemas.String("正则模式") },
            { "include", Schemas.String("包含 glob，例如 *.cs") },
            { "path", Schemas.String("起始路径") },
            { "limit", Schemas.Number("返回上限") }
        }, required: new[] { "pattern" }),
        args =>
        {
            if (args is null || !args.Value.TryGetProperty("pattern", out var pat))
            {
                return Task.FromResult("缺少 pattern");
            }

            var regex = new Regex(pat.GetString() ?? string.Empty, RegexOptions.Compiled | RegexOptions.Multiline);
            var root = args.Value.TryGetProperty("path", out var p) ? p.GetString() : Directory.GetCurrentDirectory();
            var include = args.Value.TryGetProperty("include", out var inc) ? inc.GetString() : "*";
            var limit = args.Value.TryGetProperty("limit", out var lim) ? Math.Max(1, lim.GetInt32()) : 50;

            if (!Directory.Exists(root))
            {
                return Task.FromResult($"路径不存在: {root}");
            }

            var matches = new List<string>();
            foreach (var file in Directory.EnumerateFiles(root, include, SearchOption.AllDirectories))
            {
                if (matches.Count >= limit)
                {
                    break;
                }

                try
                {
                    var content = File.ReadAllText(file);
                    if (regex.IsMatch(content))
                    {
                        matches.Add(Path.GetRelativePath(root, file));
                    }
                }
                catch (Exception ex)
                {
                    matches.Add($"<error> {file}: {ex.Message}");
                }
            }

            return Task.FromResult(ToolHelpers.Json(new { matches }));
        }
    );
}
