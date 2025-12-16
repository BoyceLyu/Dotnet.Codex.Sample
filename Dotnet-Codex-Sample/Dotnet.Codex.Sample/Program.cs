using System.Text;
using System.Text.Json;
using Dotnet.Codex.Sample;
using Dotnet.Codex.Sample.Tools;

var argsMap = ArgParser.Parse(args);
var mode = argsMap.GetValueOrDefault("mode")?.ToLowerInvariant() ?? "chat";

switch (mode)
{
	case "chat":
		await RunChatAsync(argsMap);
		break;
	case "mcp":
		await RunMcpAsync();
		break;
	default:
		Console.Error.WriteLine($"未知模式: {mode}. 可选 chat | mcp");
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
