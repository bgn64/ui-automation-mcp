#!/usr/bin/env bash
#
# Publishes ui-automation-mcp as a self-contained single-file executable
# packaged in a macOS .app bundle.
#
# Usage:
#   publish.sh                                  # writes to /Applications/UI Automation MCP.app
#   publish.sh ~/path/to/Custom.app             # writes to a custom location
#   publish.sh --version 0.1.2                  # also produces a versioned zip
#                                               # (./ui-automation-mcp-0.1.2-macos-arm64.zip)
#                                               # and emits zip=/zipName= to $GITHUB_OUTPUT
#
# When --version is supplied the .app is built in a temp directory rather than
# the user's /Applications, making it safe to run in CI.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

version=""
app_bundle=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --version)
      version="$2"
      shift 2
      ;;
    --version=*)
      version="${1#*=}"
      shift
      ;;
    -h|--help)
      # Print the leading comment block (lines starting with `#` after the
      # shebang) as our help text.
      sed -n '/^#!/d; /^[^#]/q; s/^# \{0,1\}//; p' "${BASH_SOURCE[0]}"
      exit 0
      ;;
    *)
      if [[ -n "${app_bundle}" ]]; then
        echo "Unexpected argument: $1" >&2
        exit 64
      fi
      app_bundle="$1"
      shift
      ;;
  esac
done

case "$(uname -m)" in
  arm64) runtime="osx-arm64" ;;
  x86_64) runtime="osx-x64" ;;
  *) echo "Unsupported macOS architecture: $(uname -m)" >&2; exit 1 ;;
esac

if [[ -z "${app_bundle}" ]]; then
  if [[ -n "${version}" ]]; then
    # CI / versioned-zip mode: stage in a temp dir, never touch /Applications.
    stage_dir="$(mktemp -d -t ui-automation-mcp-publish)"
    app_bundle="${stage_dir}/UI Automation MCP.app"
  else
    app_bundle="/Applications/UI Automation MCP.app"
  fi
fi

macos_dir="${app_bundle}/Contents/MacOS"

echo "Publishing ui-automation-mcp for ${runtime} ..."

# Publish to a temporary directory first
tmp_dir="$(mktemp -d)"
cleanup() {
  rm -rf "${tmp_dir}"
  if [[ -n "${stage_dir:-}" ]]; then
    rm -rf "${stage_dir}"
  fi
}
trap cleanup EXIT

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

# CFBundleVersion / CFBundleShortVersionString reflect the release version when
# supplied, so `mdls` and Finder show the right number after install.
plist_version="${version:-1.0}"

cat > "${app_bundle}/Contents/Info.plist" << EOF
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
    <string>${plist_version}</string>
    <key>CFBundleShortVersionString</key>
    <string>${plist_version}</string>
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

if [[ -n "${version}" ]]; then
  arch_label="${runtime#osx-}"
  zip_name="ui-automation-mcp-${version}-macos-${arch_label}.zip"
  zip_path="${repo_root}/${zip_name}"

  echo
  echo "Creating release zip: ${zip_path} ..."
  rm -f "${zip_path}"
  # Zip from the bundle's parent so the archive root contains "UI Automation MCP.app/".
  bundle_parent="$(dirname "${app_bundle}")"
  bundle_name="$(basename "${app_bundle}")"
  (cd "${bundle_parent}" && zip -qry "${zip_path}" "${bundle_name}")
  echo "Zip created:  ${zip_path}"

  if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
    {
      echo "zip=${zip_path}"
      echo "zipName=${zip_name}"
    } >> "${GITHUB_OUTPUT}"
  fi
else
  echo
  echo "MCP config (e.g. ~/.copilot/mcp-config.json):"
  echo "  {\"command\": \"${macos_dir}/ui-automation-mcp\", \"args\": []}"
  echo
  echo "Grant Accessibility: System Settings > Privacy & Security > Accessibility"
  echo "  Click '+' and add '${app_bundle}'"
fi
