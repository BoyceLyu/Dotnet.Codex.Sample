using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Dotnet.Codex.Sample;

internal sealed class OpenAiChatClient : IDisposable
{
	private readonly HttpClient _httpClient;
	private readonly string _model;

	private OpenAiChatClient(HttpClient httpClient, string model)
	{
		_httpClient = httpClient;
		_model = model;
	}

	public static bool TryCreateFromEnvironment(
		string? modelOverride,
		[NotNullWhen(true)] out OpenAiChatClient? client,
		out string? errorMessage)
	{
		var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
		if (string.IsNullOrWhiteSpace(apiKey))
		{
			errorMessage = "缺少 OPENAI_API_KEY 环境变量，无法调用 OpenAI 接口。";
			client = null;
			return false;
		}

		var baseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL")?.TrimEnd('/')
					  ?? "https://api.openai.com/v1";
		var model = string.IsNullOrWhiteSpace(modelOverride) ? "gpt-4.1-mini" : modelOverride;

		var httpClient = new HttpClient
		{
			BaseAddress = new Uri(baseUrl)
		};
		httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

		client = new OpenAiChatClient(httpClient, model);
		errorMessage = null;
		return true;
	}

	public async Task<string> GetChatCompletionAsync(
		string systemPrompt,
		string userMessage,
		CancellationToken cancellationToken = default)
	{
		var payload = new
		{
			model = _model,
			messages = new object[]
			{
				new { role = "system", content = systemPrompt },
				new { role = "user", content = userMessage }
			}
		};

		var json = JsonSerializer.Serialize(payload);
		using var content = new StringContent(json, Encoding.UTF8, "application/json");

		var response = await _httpClient.PostAsync("/chat/completions", content, cancellationToken);
		var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException($"OpenAI 调用失败: {response.StatusCode}\n{responseBody}");
		}

		using var document = JsonDocument.Parse(responseBody);
		var root = document.RootElement;

		return root.GetProperty("choices")[0]
			.GetProperty("message")
			.GetProperty("content")
			.GetString()
			   ?? string.Empty;
	}

	public void Dispose()
	{
		_httpClient.Dispose();
	}
}
