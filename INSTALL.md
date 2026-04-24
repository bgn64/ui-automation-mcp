# Installing ui-automation-mcp

## For distributors (building the tool)

Run the publish script from the repo root:

```powershell
.\publish.ps1
```

This produces a self-contained executable at `./publish/ui-automation-mcp.exe` that has no external dependencies — recipients don't need the .NET SDK or runtime installed.

Share the contents of the `publish/` folder (e.g., zip it up, put it on a network share, or attach it to a GitHub release).

## For users (installing the tool)

### 1. Get the files

Copy the published folder to a permanent location on your machine, for example:

```
C:\tools\ui-automation-mcp\
```

### 2. Configure your MCP client

Add the following to your MCP configuration (e.g., `mcp.json` or your client's settings file):

```json
{
  "mcpServers": {
    "ui-automation": {
      "type": "stdio",
      "command": "C:\\tools\\ui-automation-mcp\\ui-automation-mcp.exe"
    }
  }
}
```

Adjust the path to wherever you placed the files.

That's it — the MCP server will be launched automatically by your client when needed.

## Alternative: run from source

If you have the .NET 9 SDK installed and prefer to run from source:

```json
{
  "mcpServers": {
    "ui-automation": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "C:\\path\\to\\ui-automation-mcp\\src\\UIAutomation.Mcp"]
    }
  }
}
```

> **Note:** The first launch will be slower as `dotnet run` compiles the project.
