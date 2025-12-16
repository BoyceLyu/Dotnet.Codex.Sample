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
}
