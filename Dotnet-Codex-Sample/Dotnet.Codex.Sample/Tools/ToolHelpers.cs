using System.Text.Json;

namespace Dotnet.Codex.Sample.Tools;

internal static class ToolHelpers
{
    public static string RenderArgs(string prefix, JsonElement? args)
    {
        if (args is null)
        {
            return prefix;
        }

        try
        {
            return $"{prefix}: {args.Value}";
        }
        catch
        {
            return prefix;
        }
    }

    public static string FormatProcessResult(int exit, string stdout, string stderr)
    {
        return Json(new
        {
            exit_code = exit,
            stdout = stdout.TrimEnd(),
            stderr = stderr.TrimEnd()
        });
    }

    public static string Json(object obj) => JsonSerializer.Serialize(obj);

    public static string TruncateForDisplay(string text, int maxLength = 1200)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
        {
            return text;
        }

        var headLength = maxLength / 2;
        var tailLength = maxLength - headLength;

        var head = text[..headLength];
        var tail = text[^tailLength..];
        var omitted = text.Length - maxLength;

        return $"{head}\n...[截断 {omitted} 字符]...\n{tail}";
    }
}
