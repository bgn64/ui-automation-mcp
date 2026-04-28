cask "ui-automation-mcp" do
  # The first macOS bundle will be produced by the next tagged release; until
  # then the URL points at a release asset that doesn't exist yet, so we leave
  # hash verification disabled. The release workflow rewrites both `version`
  # and `sha256` whenever it publishes a new bundle.
  version "0.1.1"
  sha256 :no_check

  url "https://github.com/bgn64/ui-automation-mcp/releases/download/v#{version}/ui-automation-mcp-#{version}-macos-arm64.zip"
  name "UI Automation MCP"
  desc "MCP server exposing macOS UI Automation as tools for AI agents"
  homepage "https://github.com/bgn64/ui-automation-mcp"

  depends_on macos: ">= :sonoma"
  depends_on arch: :arm64

  app "UI Automation MCP.app"

  livecheck do
    url :url
    strategy :github_latest
  end

  caveats <<~EOS
    UI Automation MCP needs Accessibility permission to control your computer.
    Grant it once at:

      System Settings -> Privacy & Security -> Accessibility

    Click "+" and add:

      #{appdir}/UI Automation MCP.app

    Then add the following to your MCP client's configuration:

      {
        "mcpServers": {
          "ui-automation": {
            "type": "stdio",
            "command": "#{appdir}/UI Automation MCP.app/Contents/MacOS/ui-automation-mcp"
          }
        }
      }

    The bundle is ad-hoc signed (not notarized), so on first launch macOS may
    show a Gatekeeper prompt. Open System Settings -> Privacy & Security and
    click "Open Anyway" to allow it.
  EOS
end
