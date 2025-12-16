using System.Text;
using System.Text.Json;

namespace Dotnet.Codex.Sample.Tools;

internal sealed class WebSearchTool : IMcpToolProvider
{
    public McpTool Build() => new(
        "web_search",
        "网络搜索（使用 DuckDuckGo API，无需密钥）",
        Schemas.Object(new()
        {
            { "query", Schemas.String("搜索关键词") }
        }, required: new[] { "query" }),
        async args =>
        {
            if (args is null || !args.Value.TryGetProperty("query", out var q))
            {
                return "缺少 query";
            }

            var query = q.GetString() ?? string.Empty;
            using var client = new HttpClient();
            var url = $"https://api.duckduckgo.com/?q={Uri.EscapeDataString(query)}&format=json&no_html=1&skip_disambig=1";
            var json = await client.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var sb = new StringBuilder();
            if (root.TryGetProperty("Heading", out var heading))
            {
                sb.AppendLine($"Heading: {heading.GetString()}");
            }
            if (root.TryGetProperty("AbstractText", out var abs) && abs.GetString() is { Length: > 0 } absText)
            {
                sb.AppendLine(absText);
            }
            if (root.TryGetProperty("RelatedTopics", out var topics) && topics.ValueKind == JsonValueKind.Array)
            {
                var count = 0;
                foreach (var t in topics.EnumerateArray())
                {
                    if (count++ >= 5) break;
                    if (t.TryGetProperty("Text", out var txt))
                    {
                        sb.AppendLine("- " + txt.GetString());
                    }
                }
            }

            var result = sb.ToString().Trim();
            return string.IsNullOrWhiteSpace(result) ? "无结果" : result;
        }
    );
}
