# Dotnet Codex 极简教学版

一个自包含的 .NET 控制台示例，用最少代码演示：
- 调用 OpenAI Chat Completions（含中文版 Codex 系统提示词）。
- 运行简易 MCP 服务器（JSON-RPC over stdin/stdout，覆盖 Codex 工具全集的演示实现）。

## 准备
- .NET SDK 8.0+（模板使用 net10 目标框架，可按需改为 net8）。
- 配好环境变量：`OPENAI_API_KEY`（必需），`OPENAI_BASE_URL`（可选，自定义代理时使用）。

## 运行聊天示例（调用 OpenAI）
```powershell
# PowerShell 设置密钥（当前会话）
$env:OPENAI_API_KEY = "sk-..."

# 直接传入用户消息
cd Dotnet-Codex-Sample/Dotnet.Codex.Sample
 dotnet run -- --message "写一个计算斐波那契的 C# 函数"

# 或交互输入（按两次回车结束输入）
 dotnet run --
```
默认模型是 `gpt-4.1-mini`，可用 `--model` 覆盖：
```powershell
 dotnet run -- --model gpt-4.1 --message "给我一个分层架构示例"
```

## 运行 MCP 演示服务器
```powershell
cd Dotnet-Codex-Sample/Dotnet.Codex.Sample
 dotnet run -- --mode mcp
```
服务器通过 stdin/stdout 传输 JSON-RPC 消息，支持方法：
- `initialize`
- `ping`
- `tools/list`（返回所有 Codex 工具的演示版定义）
- `tools/call`

示例请求（Content-Length 头遵循 MCP/LSP 风格）：
```
Content-Length: 88

{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"time"}}
```
响应类似：
```
Content-Length: 120

{"jsonrpc":"2.0","id":1,"result":{"content":[{"type":"text","text":"2024-06-20T12:34:56.789Z"}]}}
```

## 系统提示词（中文版 Codex 风格）
```
你是 GitHub Copilot，一名资深 AI 编程助手。
- 当被问及正在使用的模型时，回答 “GPT-5.1-Codex-Max (Preview)”。
- 拒绝生成有害、仇恨、色情或暴力的内容，改为回复 "Sorry, I can't assist with that."。
- 输出保持简洁、准确，可读；提供代码时优先给出最小可运行示例。
- 遇到不确定的需求时，先澄清再行动。
```

## 关键文件
- `Program.cs`：命令行解析、OpenAI 调用、MCP 服务器实现、中文系统提示词，及 Codex 全工具的演示版 schema/handler。

## 已包含的 Codex 工具（演示版，不接触真实文件或网络）
- shell 家族：`shell`、`shell_command`、`local_shell`、`container.exec`、`exec_command`、`write_stdin`
- 计划/补丁：`update_plan`、`apply_patch`
- MCP 资源：`list_mcp_resources`、`list_mcp_resource_templates`、`read_mcp_resource`
- 文件/目录/搜索：`read_file`、`list_dir`、`grep_files`
- 视图/搜索：`view_image`、`web_search`
- 测试/同步：`test_sync_tool`
- 演示辅助：`time`、`echo`

## 常见问题
- 401 或 403：检查 `OPENAI_API_KEY` 与 `OPENAI_BASE_URL`。
- 连接超时：确认代理或网络访问是否允许直连 OpenAI。
- MCP 未响应：确保按协议发送 `Content-Length` 头与正确的 JSON。
