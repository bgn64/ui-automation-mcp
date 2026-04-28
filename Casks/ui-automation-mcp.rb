cask "ui-automation-mcp" do
  # The release workflow rewrites both `version` and the `sha256` line below
  # whenever it publishes a tagged release. Until the first macOS bundle has
  # been published, `sha256 :no_check` keeps `brew install` from failing
  # against a missing artifact.
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
