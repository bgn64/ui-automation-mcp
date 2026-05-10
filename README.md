# ui-automation-mcp

An MCP (Model Context Protocol) server that exposes OS-level desktop accessibility automation as tools — letting AI agents find, inspect, and interact with desktop application UIs on Windows and macOS.

## What is this for?

This MCP enables **UI automation driven by a large language model**. It gives an AI agent the ability to see, navigate, and operate desktop applications — clicking buttons, filling in forms, reading text, and more — just as a human would.

It is **not intended to replace any first-party MCPs**. Where a dedicated MCP already exists for an application or service, prefer that. This server is meant to **bridge the gaps** where first-party tool support does not yet exist, giving agents a general-purpose fallback for interacting with arbitrary desktop UIs.

> **Tip — skill files for repeat workflows:** The first time an agent navigates a UI through this MCP it must explore and discover elements step-by-step, which can be slow. Once it has successfully completed a workflow, you can have the agent **write a skill file for itself** that encodes the steps it learned. On subsequent runs the agent can replay that skill directly, **significantly improving performance** for the same UI workflow.

## Features

Built with .NET 9, Windows UI Automation, and the macOS Accessibility/CoreGraphics APIs, it provides tools for:

- **Window discovery** — list and manage top-level windows
- **Element search** — find elements by name, automation ID, control type, and more
- **Interaction** — click, type, toggle, select, expand/collapse, scroll, send keystrokes
- **Inspection** — read element properties, values, text content, grid/table data
- **Screen capture** — take screenshots (full screen or per-monitor)
- **Advanced queries** — flexible filtered search with depth control, pattern matching, and property projection

## Architecture

The MCP tool surface is shared across platforms. It calls platform-neutral services and models, which then delegate to the active OS backend:

```text
UIAutomation.Mcp/Tools
  -> UIAutomation.Core/Shared/Services
  -> UIAutomation.Core/Shared/Backends
  -> UIAutomation.Core/Platforms/MacOS or UIAutomation.Core/Platforms/Windows
  -> native OS automation APIs
  -> UIAutomation.Core/Shared/Models
```

- `Shared/Models/` contains platform-neutral DTOs only; it does not know about macOS, Windows, or future platforms.
- `Shared/Services/` contains the shared front-end contracts and delegating services used by MCP tools.
- `Shared/Backends/` contains the backend contracts implemented by each platform.
- `Platforms/MacOS/` and `Platforms/Windows/` contain all OS-specific native interop and behavior.

## Installation

### Windows: install with Scoop (recommended)

[Scoop](https://scoop.sh/) is a command-line installer for Windows.

```powershell
scoop bucket add ui-automation-mcp https://github.com/bgn64/ui-automation-mcp
scoop install ui-automation-mcp
```

This installs a self-contained `ui-automation-mcp.exe` and adds it to your `PATH` — no .NET SDK or runtime is required.

To upgrade later:

```powershell
scoop update ui-automation-mcp
```

### macOS: install with Homebrew (recommended)

[Homebrew](https://brew.sh/) is a command-line installer for macOS.

```bash
brew tap bgn64/ui-automation-mcp https://github.com/bgn64/ui-automation-mcp
brew install --cask ui-automation-mcp
```

This downloads a self-contained `.app` bundle to `/Applications` — no .NET SDK or runtime is required.

To upgrade later:

```bash
brew upgrade --cask ui-automation-mcp
```

> **First-launch note**: the bundle is ad-hoc signed (not notarized), so macOS may show a Gatekeeper prompt the first time the MCP client launches it. Open **System Settings → Privacy & Security** and click **"Open Anyway"** to allow it.
>
> **Accessibility permission** is required to control your computer. Grant it once at **System Settings → Privacy & Security → Accessibility**, click **+**, and add `/Applications/UI Automation MCP.app`.

### macOS: build from source

If you'd rather not use Homebrew, clone the repo and run:

```bash
./publish.sh                     # writes ./publish/UI Automation MCP.app
./publish.sh --output-dir ~/apps # or choose your own directory
```

Then drag the `.app` bundle to `/Applications` (or anywhere else you'd like to
keep it). The bundle contains a single self-contained executable; no .NET SDK
or runtime installation is required.

macOS requires a `.app` bundle (rather than a bare executable) to grant
Accessibility permissions.

### Add the MCP server to your configuration

Add the following entry to your MCP client's JSON configuration file:

Windows:

```json
{
  "mcpServers": {
    "ui-automation": {
      "type": "stdio",
      "command": "ui-automation-mcp"
    }
  }
}
```

macOS:

```json
{
  "mcpServers": {
    "ui-automation": {
      "type": "stdio",
      "command": "/Applications/UI Automation MCP.app/Contents/MacOS/ui-automation-mcp"
    }
  }
}
```

> **Adjust the path** to match wherever you installed the `.app` bundle.

### Where do I find my MCP configuration file?

The location depends on which MCP client you're using:

| Client | Config file location |
|--------|---------------------|
| **VS Code (GitHub Copilot)** | `.vscode/mcp.json` in your workspace, or `%APPDATA%\Code\User\settings.json` (under `"mcp"` key) |
| **Visual Studio** | `%LOCALAPPDATA%\Microsoft\VisualStudio\<version>\copilot\mcp.json` |
| **Claude Desktop** | `%APPDATA%\Claude\claude_desktop_config.json` |
| **Copilot CLI** | `~/.copilot/config/mcp.json` |
| **Cursor** | `~/.cursor/mcp.json` |

If the file doesn't exist yet, create it with the JSON blob above. If it already exists, merge the `"ui-automation"` entry into the existing `"mcpServers"` object.

After saving the config, restart your MCP client (or reload the window in VS Code). The server will be launched automatically when the client needs it.

## Alternative installation methods

### Manual zip download

If you'd rather not use Scoop, download the latest release zip from the [releases page](https://github.com/bgn64/ui-automation-mcp/releases) and extract it to a permanent folder, e.g. `C:\tools\ui-automation-mcp\`. Then point your MCP client at the absolute path:

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

The zip is self-contained — no .NET SDK or runtime installation is required.

### Run from source

If you have the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) installed and prefer to run from source, clone the repo and point your MCP config at the project directly.

Windows:

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

macOS:

```json
{
  "mcpServers": {
    "ui-automation": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/ui-automation-mcp/src/UIAutomation.Mcp"]
    }
  }
}
```

> **Note:** The first launch will be slower as `dotnet run` compiles the project.

## Requirements

- **Windows** — uses Windows UI Automation and Win32 APIs.
- **macOS** — uses Accessibility (`AXUIElement`), CoreGraphics (`CGEvent`, `CGDisplay`), and ImageIO.

### macOS permissions

macOS requires privacy permissions for desktop automation:

1. Open **System Settings > Privacy & Security > Accessibility**, click "+", and add the `.app` bundle (e.g. `/Applications/UI Automation MCP.app`).
2. Open **System Settings > Privacy & Security > Screen & System Audio Recording** and allow the same `.app` bundle if screenshots should include screen contents.

> macOS grants Accessibility trust per-binary. A `.app` bundle (created by `publish.sh`) is required — macOS will not list a bare executable in the Accessibility settings.

## Publishing from source

Windows:

```powershell
.\publish.ps1                       # writes .\publish\
.\publish.ps1 -OutputDir C:\tools   # or choose your own directory
```

macOS:

```bash
./publish.sh                         # writes ./publish/UI Automation MCP.app
./publish.sh --output-dir ~/apps     # or choose your own directory
```

The macOS publish is self-contained and single-file, so the app bundle should contain
only `Contents/MacOS/ui-automation-mcp` plus the standard bundle metadata.
