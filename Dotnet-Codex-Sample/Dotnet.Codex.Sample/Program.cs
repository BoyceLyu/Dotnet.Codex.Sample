using System.Linq;
using System.Text;
using System.Text.Json;
using Dotnet.Codex.Sample;
using Dotnet.Codex.Sample.Tools;

const string ModeChat = "chat";
const string ModeMcp = "mcp";
const string ModeTrun = "trun";
const string ModeReact = "react";

var argsMap = ArgParser.Parse(args);
var mode = argsMap.GetValueOrDefault("mode")?.ToLowerInvariant() ?? ModeChat;

switch (mode)
{
        case ModeChat:
                await RunChatAsync(argsMap);
                break;
        case ModeMcp:
                await RunMcpAsync();
                break;
        case ModeTrun:
                await RunToolRunAsync(argsMap);
                break;
        case ModeReact:
                await RunReactAsync(argsMap);
                break;
        default:
                Console.Error.WriteLine($"未知模式: {mode}. 可选 chat | mcp | trun | react");
                break;
}

static async Task RunChatAsync(Dictionary<string, string?> argsMap)
{
        if (!OpenAiChatClient.TryCreateFromEnvironment(
                    argsMap.GetValueOrDefault("model"),
                    out var client,
                    out var error))
        {
                Console.Error.WriteLine(error);
                return;
        }

        var userMessage = ReadUserMessage(argsMap);

        if (string.IsNullOrWhiteSpace(userMessage))
        {
                Console.Error.WriteLine("未提供用户输入，结束。");
                return;
        }

        using var chatClient = client;
        string content;
        try
        {
		content = await chatClient.GetChatCompletionAsync(Prompts.SystemPromptZh, userMessage);
	}
	catch (Exception ex)
	{
		Console.Error.WriteLine(ex.Message);
		return;
	}

	Console.WriteLine("=== AI 回复 ===");
	Console.WriteLine(content);
}

static async Task RunMcpAsync()
{
        Console.Error.WriteLine("启动 MCP 演示服务器，使用 STDIN/STDOUT 传输 JSON-RPC。");
        var server = new McpServer(ToolCatalog.All());
        await server.RunAsync();
}

static async Task RunToolRunAsync(Dictionary<string, string?> argsMap)
{
        var toolName = argsMap.GetValueOrDefault("tool");
        if (string.IsNullOrWhiteSpace(toolName))
        {
                Console.Error.WriteLine("trun 模式需要传入 --tool <工具名>。可用工具请查看 README 或 tools/list 接口。");
                return;
        }

        var tools = ToolCatalog.All();
        var tool = tools.FirstOrDefault(t => string.Equals(t.Name, toolName, StringComparison.OrdinalIgnoreCase));
        if (tool is null)
        {
                Console.Error.WriteLine($"未找到工具: {toolName}");
                return;
        }

        JsonElement? arguments = null;
        var rawArgs = argsMap.GetValueOrDefault("args");
        if (!string.IsNullOrWhiteSpace(rawArgs))
        {
                if (!TryParseJson(rawArgs, out var parsedArgs))
                {
                        Console.Error.WriteLine("无法解析 --args JSON，示例：--args '{\"path\":\"foo.txt\"}'");
                        return;
                }
                arguments = parsedArgs.RootElement.Clone();
        }

        var result = await tool.Handler(arguments);
        Console.WriteLine("=== 工具输出 ===");
        Console.WriteLine(result);
}

static async Task RunReactAsync(Dictionary<string, string?> argsMap)
{
        if (!OpenAiChatClient.TryCreateFromEnvironment(
                    argsMap.GetValueOrDefault("model"),
                    out var client,
                    out var error))
        {
                Console.Error.WriteLine(error);
                return;
        }

        var userMessage = ReadUserMessage(argsMap);
        if (string.IsNullOrWhiteSpace(userMessage))
        {
                Console.Error.WriteLine("未提供用户输入，结束。");
                return;
        }

        var tools = ToolCatalog.All();
        var messages = new List<ChatMessage>
        {
                new("system", Prompts.BuildReactSystemPrompt(tools)),
                new("user", userMessage)
        };

        using var chatClient = client;

        while (true)
        {
                string assistantMessage;
                try
                {
                        assistantMessage = await chatClient.GetChatCompletionAsync(messages);
                }
                catch (Exception ex)
                {
                        Console.Error.WriteLine(ex.Message);
                        return;
                }

                if (!TryParseJson(assistantMessage, out var json))
                {
                        Console.Error.WriteLine($"LLM 回复无法解析为 JSON: {assistantMessage}");
                        return;
                }

                var root = json.RootElement;

                if (root.TryGetProperty("final", out var finalMessage) && finalMessage.ValueKind == JsonValueKind.String)
                {
                        Console.WriteLine("=== AI 最终回复 ===");
                        Console.WriteLine(finalMessage.GetString());
                        return;
                }

                if (!root.TryGetProperty("action", out var actionProperty))
                {
                        Console.Error.WriteLine($"缺少 action 字段: {assistantMessage}");
                        return;
                }

                var actionName = actionProperty.GetString();
                if (string.IsNullOrWhiteSpace(actionName))
                {
                        Console.Error.WriteLine($"action 字段为空: {assistantMessage}");
                        return;
                }

                var tool = tools.FirstOrDefault(t => string.Equals(t.Name, actionName, StringComparison.OrdinalIgnoreCase));
                if (tool is null)
                {
                        Console.Error.WriteLine($"未找到工具: {actionName}");
                        return;
                }

                JsonElement? args = null;
                if (root.TryGetProperty("input", out var inputProperty))
                {
                        args = inputProperty;
                }

                string observation;
                try
                {
                        observation = await tool.Handler(args);
                }
                catch (Exception ex)
                {
                        observation = $"工具执行异常: {ex.Message}";
                }

                messages.Add(new ChatMessage("assistant", assistantMessage));
                messages.Add(new ChatMessage("user", $"工具 {actionName} 输出:\n{ToolHelpers.TruncateForDisplay(observation)}"));
        }
}

internal static class ArgParser
{
	public static Dictionary<string, string?> Parse(string[] args)
	{
		var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
		for (var i = 0; i < args.Length; i++)
		{
			if (!args[i].StartsWith("--"))
			{
				continue;
			}

			var key = args[i][2..];
			string? value = null;
			if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
			{
				value = args[i + 1];
				i++;
			}

			dict[key] = value;
		}

		return dict;
	}
}

internal static class Prompts
{
        public const string SystemPromptZh =
                """
                你是 GitHub Copilot，一名资深 AI 编程助手。
                - 当被问及正在使用的模型时，回答 “GPT-5.1-Codex-Max (Preview)”。
                - 拒绝生成有害、仇恨、色情或暴力的内容，改为回复 "Sorry, I can't assist with that."。
                - 输出保持简洁、准确，可读；提供代码时优先给出最小可运行示例。
                - 遇到不确定的需求时，先澄清再行动。
                """;

        public static string BuildReactSystemPrompt(IEnumerable<McpTool> tools)
        {
                var toolsList = string.Join('\n', tools.Select(t => $"- {t.Name}: {t.Description}"));

                return
                        $"""
你是 GitHub Copilot，一名资深 AI 编程助手。可以使用下列工具解决任务：
{toolsList}

请遵循 ReAct 思路：先在心里推理，再决定是否调用工具。回复必须是 JSON 对象且不要出现额外文字：
- 调用工具时，返回 {{"action":"<tool_name>","input":{{...}}}}
- 完成任务时，返回 {{"final":"<答案文本>"}}
"""";
        }
}

static string? ReadUserMessage(Dictionary<string, string?> argsMap)
{
        var userMessage = argsMap.GetValueOrDefault("message");

        if (string.IsNullOrWhiteSpace(userMessage))
        {
                Console.WriteLine("请输入用户问题，然后按回车（空行结束）：");
                var sb = new StringBuilder();
                string? line;
                while (!string.IsNullOrEmpty(line = Console.ReadLine()))
                {
                        sb.AppendLine(line);
                }

                userMessage = sb.ToString().Trim();
        }

        return userMessage;
}

static bool TryParseJson(string content, out JsonDocument document)
{
        var start = content.IndexOf('{');
        var end = content.LastIndexOf('}');

        if (start < 0 || end < start)
        {
                document = null!;
                return false;
        }

        var json = content[start..(end + 1)];

        try
        {
                document = JsonDocument.Parse(json);
                return true;
        }
        catch (JsonException)
        {
                document = null!;
                return false;
        }
}

internal sealed class McpServer
{
	private readonly IReadOnlyDictionary<string, McpTool> _tools;

	public McpServer(IEnumerable<McpTool> tools)
	{
		_tools = tools.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
	}

	public async Task RunAsync()
	{
		using var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);
		using var writer = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false))
		{
			AutoFlush = true
		};

		while (true)
		{
			var contentLength = await ReadContentLengthAsync(reader);
			if (contentLength is null)
			{
				break;
			}

			var payload = await ReadPayloadAsync(reader, contentLength.Value);
			if (string.IsNullOrWhiteSpace(payload))
			{
				continue;
			}

			using var doc = JsonDocument.Parse(payload);
			var root = doc.RootElement;
			if (!root.TryGetProperty("id", out var idProperty))
			{
				continue; // Notification without id
			}

			var method = root.GetProperty("method").GetString();
			var response = method switch
			{
				"initialize" => BuildInitialize(idProperty),
				"ping" => BuildResult(idProperty, new { message = "pong" }),
				"tools/list" => BuildToolsList(idProperty),
				"tools/call" => await HandleToolCallAsync(idProperty, root),
				_ => BuildError(idProperty, $"未知方法: {method}")
			};

			var responseJson = JsonSerializer.Serialize(response);
			await writer.WriteAsync($"Content-Length: {Encoding.UTF8.GetByteCount(responseJson)}\r\n\r\n{responseJson}");
		}
	}

	private static async Task<int?> ReadContentLengthAsync(StreamReader reader)
	{
		string? line;
		int? length = null;
		while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
		{
			if (line.StartsWith("Content-Length", StringComparison.OrdinalIgnoreCase))
			{
				var parts = line.Split(':', 2, StringSplitOptions.TrimEntries);
				if (parts.Length == 2 && int.TryParse(parts[1], out var parsed))
				{
					length = parsed;
				}
			}
		}

		return length;
	}

	private static async Task<string> ReadPayloadAsync(StreamReader reader, int length)
	{
		var buffer = new char[length];
		var read = 0;
		while (read < length)
		{
			var current = await reader.ReadAsync(buffer, read, length - read);
			if (current == 0)
			{
				break;
			}
			read += current;
		}

		return new string(buffer, 0, read);
	}

	private object BuildInitialize(JsonElement id) => new
	{
		jsonrpc = "2.0",
		id = id,
		result = new
		{
			serverInfo = new { name = "dotnet-codex-mcp-demo", version = "0.1.0" },
			capabilities = new
			{
				tools = new { listChanged = true }
			}
		}
	};

	private object BuildToolsList(JsonElement id)
	{
		var tools = _tools.Values.Select(t => new
		{
			name = t.Name,
			description = t.Description,
			input_schema = t.InputSchema
		});

		return new
		{
			jsonrpc = "2.0",
			id = id,
			result = new { tools }
		};
	}

	private async Task<object> HandleToolCallAsync(JsonElement id, JsonElement request)
	{
		if (!request.TryGetProperty("params", out var @params))
		{
			return BuildError(id, "缺少 params 字段");
		}

		var name = @params.GetProperty("name").GetString();
		if (name is null || !_tools.TryGetValue(name, out var tool))
		{
			return BuildError(id, $"未找到工具: {name}");
		}

		JsonElement? args = null;
		if (@params.TryGetProperty("arguments", out var arguments))
		{
			args = arguments;
		}

		var result = await tool.Handler(args);
		return BuildResult(id, new { content = new[] { new { type = "text", text = result } } });
	}

	private object BuildResult(JsonElement id, object result) => new
	{
		jsonrpc = "2.0",
		id = id,
		result
	};

	private object BuildError(JsonElement id, string message) => new
	{
		jsonrpc = "2.0",
		id = id,
		error = new { code = -32000, message }
	};
}

internal sealed record McpTool(
	string Name,
	string Description,
	JsonElement InputSchema,
	Func<JsonElement?, Task<string>> Handler);
