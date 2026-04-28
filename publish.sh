#!/usr/bin/env bash
#
# Publishes ui-automation-mcp as a self-contained single-file executable
# packaged in a macOS .app bundle.
#
# Parameters:
#   --output-dir <path>  Directory to publish to. Defaults to ./publish.
#   --version <ver>      Optional version string. When provided, also produces
#                        ./ui-automation-mcp-<ver>-macos-<arch>.zip and writes
#                        zip=/zipName= to $GITHUB_OUTPUT under GitHub Actions.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

output_dir="${repo_root}/publish"
version=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --output-dir)   output_dir="$2"; shift 2 ;;
    --output-dir=*) output_dir="${1#*=}"; shift ;;
    --version)      version="$2"; shift 2 ;;
    --version=*)    version="${1#*=}"; shift ;;
    *) echo "Unexpected argument: $1" >&2; exit 64 ;;
  esac
done

case "$(uname -m)" in
  arm64) runtime="osx-arm64" ;;
  x86_64) runtime="osx-x64" ;;
  *) echo "Unsupported macOS architecture: $(uname -m)" >&2; exit 1 ;;
esac

app_bundle="${output_dir}/UI Automation MCP.app"
macos_dir="${app_bundle}/Contents/MacOS"

echo "Publishing ui-automation-mcp to ${output_dir} ..."

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
echo "Published successfully to: ${output_dir}"
echo "Bundle:     ${app_bundle}"
echo "Executable: ${macos_dir}/ui-automation-mcp"

if [[ -n "${version}" ]]; then
  arch_label="${runtime#osx-}"
  zip_name="ui-automation-mcp-${version}-macos-${arch_label}.zip"
  zip_path="${repo_root}/${zip_name}"

  echo
  echo "Creating zip: ${zip_path} ..."
  rm -f "${zip_path}"
  # Zip from the bundle's parent so the archive root contains "UI Automation MCP.app/".
  bundle_parent="$(dirname "${app_bundle}")"
  bundle_name="$(basename "${app_bundle}")"
  (cd "${bundle_parent}" && zip -qry "${zip_path}" "${bundle_name}")
  echo "Zip created: ${zip_path}"

  if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
    {
      echo "zip=${zip_path}"
      echo "zipName=${zip_name}"
    } >> "${GITHUB_OUTPUT}"
  fi
fi
