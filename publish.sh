#!/usr/bin/env bash
set -euo pipefail

app_bundle="${1:-"/Applications/UI Automation MCP.app"}"
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

case "$(uname -m)" in
  arm64) runtime="osx-arm64" ;;
  x86_64) runtime="osx-x64" ;;
  *) echo "Unsupported macOS architecture: $(uname -m)" >&2; exit 1 ;;
esac

macos_dir="${app_bundle}/Contents/MacOS"

echo "Publishing ui-automation-mcp for ${runtime} ..."

# Publish to a temporary directory first
tmp_dir="$(mktemp -d)"
trap 'rm -rf "${tmp_dir}"' EXIT

dotnet publish "${repo_root}/src/UIAutomation.Mcp" \
  --nologo \
  -v quiet \
  -c Release \
  -r "${runtime}" \
  --self-contained \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true \
  -p:DebugType=None \
  -p:DebugSymbols=false \
  -o "${tmp_dir}"

# Create .app bundle structure
mkdir -p "${macos_dir}"
mkdir -p "${app_bundle}/Contents/Resources"

cat > "${app_bundle}/Contents/Info.plist" << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleIdentifier</key>
    <string>com.github.ui-automation-mcp</string>
    <key>CFBundleName</key>
    <string>UI Automation MCP</string>
    <key>CFBundleExecutable</key>
    <string>ui-automation-mcp</string>
    <key>CFBundleVersion</key>
    <string>1.0</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>LSUIElement</key>
    <true/>
</dict>
</plist>
EOF

# Copy published files into the bundle
rm -rf "${macos_dir:?}"/*
cp -R "${tmp_dir}/"* "${macos_dir}/"

# Ad-hoc code sign so macOS recognises the bundle for Accessibility
codesign --force --deep --sign - "${app_bundle}"

echo
echo "Published to: ${app_bundle}"
echo "Executable:   ${macos_dir}/ui-automation-mcp"
echo "Bundle files: $(find "${macos_dir}" -maxdepth 1 -type f | wc -l | tr -d ' ')"
echo
echo "MCP config (e.g. ~/.copilot/mcp-config.json):"
echo "  {\"command\": \"${macos_dir}/ui-automation-mcp\", \"args\": []}"
echo
echo "Grant Accessibility: System Settings > Privacy & Security > Accessibility"
echo "  Click '+' and add '${app_bundle}'"
