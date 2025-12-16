using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace Dotnet.Codex.Sample.Tools;

internal static class ExecSessionManager
{
    private sealed class ExecSession
    {
        public int Id { get; init; }
        public Process Process { get; init; } = default!;
        public StringBuilder Output { get; } = new();
        public object LockObj { get; } = new();
    }

    private static readonly ConcurrentDictionary<int, ExecSession> Sessions = new();
    private static int _nextId = 1;

    public static int Start(string cmd, string? workdir = null, string? shell = null, bool login = false)
    {
        var id = Interlocked.Increment(ref _nextId);
        var process = CreateProcess(cmd, workdir, shell, login);

        var session = new ExecSession { Id = id, Process = process };
        Sessions[id] = session;

        process.OutputDataReceived += (_, e) => AppendOutput(session, e.Data);
        process.ErrorDataReceived += (_, e) => AppendOutput(session, e.Data);
        process.Exited += (_, _) => AppendOutput(session, "<process exited>\n");

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return id;
    }

    public static async Task<string> WriteAsync(int sessionId, string? input, int yieldTimeMs)
    {
        if (!Sessions.TryGetValue(sessionId, out var session))
        {
            return $"会话 {sessionId} 不存在";
        }

        if (input is not null)
        {
            await session.Process.StandardInput.WriteAsync(input);
            await session.Process.StandardInput.FlushAsync();
        }

        await Task.Delay(Math.Max(0, yieldTimeMs));
        return DrainOutput(session);
    }

    private static string DrainOutput(ExecSession session)
    {
        lock (session.LockObj)
        {
            var text = session.Output.ToString();
            session.Output.Clear();
            return text;
        }
    }

    private static void AppendOutput(ExecSession session, string? text)
    {
        if (text is null)
        {
            return;
        }

        lock (session.LockObj)
        {
            session.Output.AppendLine(text);
        }
    }

    private static Process CreateProcess(string cmd, string? workdir, string? shell, bool login)
    {
        // Windows default: use cmd.exe /c
        var fileName = shell ?? "cmd.exe";
        var arguments = shell is null
            ? $"/c {cmd}"
            : cmd;

        if (shell is not null && login)
        {
            // No-op for Windows; kept for interface completeness
        }

        return new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workdir ?? Directory.GetCurrentDirectory()
            },
            EnableRaisingEvents = true
        };
    }
}
