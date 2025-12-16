using System.Diagnostics;

namespace Dotnet.Codex.Sample.Tools;

internal static class ProcessRunner
{
    public static async Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(
        string fileName,
        string arguments,
        string? workdir = null,
        int? timeoutMs = null,
        string? input = null)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workdir ?? Directory.GetCurrentDirectory()
            }
        };

        process.Start();

        if (!string.IsNullOrEmpty(input))
        {
            await process.StandardInput.WriteAsync(input);
            process.StandardInput.Close();
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        var waitTask = process.WaitForExitAsync();

        if (timeoutMs is int t)
        {
            var completed = await Task.WhenAny(waitTask, Task.Delay(t));
            if (completed != waitTask && !process.HasExited)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // ignore
                }
            }
        }
        else
        {
            await waitTask;
        }

        await Task.WhenAll(stdoutTask, stderrTask);
        return (process.ExitCode, await stdoutTask, await stderrTask);
    }
}