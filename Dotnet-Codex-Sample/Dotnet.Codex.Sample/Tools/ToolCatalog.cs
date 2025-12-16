namespace Dotnet.Codex.Sample.Tools;

internal static class ToolCatalog
{
    public static IReadOnlyList<McpTool> All()
    {
        IMcpToolProvider[] providers =
        {
            new UpdatePlanTool(),
            new ApplyPatchTool(),
            new ShellTool(),
            new ShellCommandTool(),
            new LocalShellTool(),
            new ExecCommandTool(),
            new WriteStdinTool(),
            new ContainerExecTool(),
            new ListMcpResourcesTool(),
            new ListMcpResourceTemplatesTool(),
            new ReadMcpResourceTool(),
            new ReadFileTool(),
            new ListDirTool(),
            new GrepFilesTool(),
            new ViewImageTool(),
            new WebSearchTool(),
            new TestSyncTool(),
            new TimeTool(),
            new EchoTool()
        };

        return providers.Select(p => p.Build()).ToList();
    }
}
