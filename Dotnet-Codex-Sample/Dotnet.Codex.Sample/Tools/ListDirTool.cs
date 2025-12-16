using System.Text.Json;

namespace Dotnet.Codex.Sample.Tools;

internal sealed class ListDirTool : IMcpToolProvider
{
    public McpTool Build() => new(
        "list_dir",
        "列目录（真实 IO，返回 JSON 列表）",
        Schemas.Object(new()
        {
            { "dir_path", Schemas.String("目录路径") },
            { "offset", Schemas.Number("起始序号") },
            { "limit", Schemas.Number("条数") },
            { "depth", Schemas.Number("深度") }
        }, required: new[] { "dir_path" }),
        args =>
        {
            if (args is null || !args.Value.TryGetProperty("dir_path", out var dp))
            {
                return Task.FromResult("缺少 dir_path");
            }

            var path = dp.GetString() ?? string.Empty;
            if (!Directory.Exists(path))
            {
                return Task.FromResult($"目录不存在: {path}");
            }

            var offset = args.Value.TryGetProperty("offset", out var off) ? Math.Max(1, off.GetInt32()) : 1;
            var limit = args.Value.TryGetProperty("limit", out var lim) ? Math.Max(1, lim.GetInt32()) : 200;
            var depth = args.Value.TryGetProperty("depth", out var dep) ? Math.Max(1, dep.GetInt32()) : 1;

            var entries = new List<string>();
            Enumerate(path, depth, entries, path);

            var slice = entries.Skip(offset - 1).Take(limit).ToArray();
            if (slice.Length == 0)
            {
                return Task.FromResult(ToolHelpers.Json(new { entries = Array.Empty<object>() }));
            }

            return Task.FromResult(ToolHelpers.Json(new { entries = slice.Select((p, idx) => new { index = offset + idx, path = p }) }));

            static void Enumerate(string dir, int depthLeft, List<string> acc, string root)
            {
                try
                {
                    foreach (var d in Directory.GetDirectories(dir))
                    {
                        acc.Add(Rel(d));
                        if (depthLeft > 1)
                        {
                            Enumerate(d, depthLeft - 1, acc, root);
                        }
                    }
                    foreach (var f in Directory.GetFiles(dir))
                    {
                        acc.Add(Rel(f));
                    }
                }
                catch (Exception ex)
                {
                    acc.Add($"<error> {dir}: {ex.Message}");
                }
            }

            string Rel(string p) => Path.GetRelativePath(root, p);
        }
    );
}
