#!/usr/bin/env bash
# Build a self-contained macOS application bundle (and optional DMG) for the csv-validate client.
#
# The bundle embeds a self-contained, single-file publish of the .NET validation engine in
# Contents/Resources/engine so end users do not need the .NET runtime installed.
#
# Usage:
#   client/scripts/bundle-macos.sh [--arch arm64|x64|universal] [--dmg] [--out <dir>]
#
# Defaults: host architecture, no DMG, output to client/dist.
set -euo pipefail

export PATH="$PATH:/usr/local/share/dotnet:$HOME/.cargo/bin"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CLIENT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
REPO_ROOT="$(cd "$CLIENT_DIR/.." && pwd)"

APP_NAME="CSV Validate"
BUNDLE_ID="com.seanr89.csv-validate"
BIN_NAME="csv-validate-client"
VERSION="$(grep -m1 '^version' "$CLIENT_DIR/Cargo.toml" | sed -E 's/version *= *"([^"]+)"/\1/')"

ARCH=""
MAKE_DMG=0
OUT_DIR="$CLIENT_DIR/dist"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --arch) ARCH="$2"; shift 2 ;;
    --dmg) MAKE_DMG=1; shift ;;
    --out) OUT_DIR="$2"; shift 2 ;;
    -h|--help) sed -n '2,11p' "$0"; exit 0 ;;
    *) echo "Unknown argument: $1" >&2; exit 2 ;;
  esac
done

if [[ "$(uname -s)" != "Darwin" ]]; then
  echo "This script must run on macOS." >&2
  exit 1
fi

if [[ -z "$ARCH" ]]; then
  case "$(uname -m)" in
    arm64) ARCH="arm64" ;;
    *) ARCH="x64" ;;
  esac
fi

rust_target() { [[ "$1" == "arm64" ]] && echo "aarch64-apple-darwin" || echo "x86_64-apple-darwin"; }
dotnet_rid() { [[ "$1" == "arm64" ]] && echo "osx-arm64" || echo "osx-x64"; }

case "$ARCH" in
  arm64|x64) ARCHES=("$ARCH") ;;
  universal) ARCHES=("arm64" "x64") ;;
  *) echo "Unsupported --arch '$ARCH' (use arm64, x64 or universal)" >&2; exit 2 ;;
esac

WORK_DIR="$(mktemp -d)"
trap 'rm -rf "$WORK_DIR"' EXIT

APP_DIR="$OUT_DIR/$APP_NAME.app"
echo "==> Building $APP_NAME $VERSION ($ARCH)"
rm -rf "$APP_DIR"
mkdir -p "$APP_DIR/Contents/MacOS" "$APP_DIR/Contents/Resources/engine"

# --- Rust client -------------------------------------------------------------
CLIENT_BINS=()
for a in "${ARCHES[@]}"; do
  target="$(rust_target "$a")"
  rustup target add "$target" >/dev/null 2>&1 || true
  echo "==> cargo build --release --target $target"
  cargo build --release --manifest-path "$CLIENT_DIR/Cargo.toml" --target "$target"
  CLIENT_BINS+=("$CLIENT_DIR/target/$target/release/$BIN_NAME")
done
if [[ ${#CLIENT_BINS[@]} -gt 1 ]]; then
  lipo -create -output "$APP_DIR/Contents/MacOS/$BIN_NAME" "${CLIENT_BINS[@]}"
else
  cp "${CLIENT_BINS[0]}" "$APP_DIR/Contents/MacOS/$BIN_NAME"
fi

# --- .NET engine (self-contained single file) --------------------------------
ENGINE_BINS=()
for a in "${ARCHES[@]}"; do
  rid="$(dotnet_rid "$a")"
  out="$WORK_DIR/engine-$rid"
  echo "==> dotnet publish ($rid, self-contained)"
  dotnet publish "$REPO_ROOT/validator/validator.csproj" -c Release -r "$rid" --self-contained true \
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None \
    -o "$out" >/dev/null
  ENGINE_BINS+=("$out/validator")
done
first_engine_dir="$(dirname "${ENGINE_BINS[0]}")"
cp -R "$first_engine_dir/Specifications" "$APP_DIR/Contents/Resources/engine/"
cp "$first_engine_dir/appsettings.json" "$APP_DIR/Contents/Resources/engine/"
if [[ ${#ENGINE_BINS[@]} -gt 1 ]]; then
  lipo -create -output "$APP_DIR/Contents/Resources/engine/validator" "${ENGINE_BINS[@]}"
else
  cp "${ENGINE_BINS[0]}" "$APP_DIR/Contents/Resources/engine/validator"
fi
chmod +x "$APP_DIR/Contents/Resources/engine/validator" "$APP_DIR/Contents/MacOS/$BIN_NAME"

# --- Info.plist --------------------------------------------------------------
cat > "$APP_DIR/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key><string>$APP_NAME</string>
  <key>CFBundleDisplayName</key><string>$APP_NAME</string>
  <key>CFBundleIdentifier</key><string>$BUNDLE_ID</string>
  <key>CFBundleVersion</key><string>$VERSION</string>
  <key>CFBundleShortVersionString</key><string>$VERSION</string>
  <key>CFBundleExecutable</key><string>$BIN_NAME</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>LSMinimumSystemVersion</key><string>11.0</string>
  <key>NSHighResolutionCapable</key><true/>
  <key>LSApplicationCategoryType</key><string>public.app-category.developer-tools</string>
  <key>CFBundleDocumentTypes</key>
  <array>
    <dict>
      <key>CFBundleTypeName</key><string>Delimited text file</string>
      <key>CFBundleTypeRole</key><string>Viewer</string>
      <key>LSHandlerRank</key><string>Alternate</string>
      <key>LSItemContentTypes</key>
      <array>
        <string>public.comma-separated-values-text</string>
        <string>public.tab-separated-values-text</string>
        <string>public.plain-text</string>
      </array>
    </dict>
  </array>
</dict>
</plist>
PLIST

# Ad-hoc sign so Gatekeeper allows local launches (use a Developer ID for distribution).
if command -v codesign >/dev/null 2>&1; then
  codesign --force --sign - "$APP_DIR/Contents/Resources/engine/validator" >/dev/null 2>&1 || true
  codesign --force --deep --sign - "$APP_DIR" >/dev/null 2>&1 || true
fi

echo "==> Smoke test: bundled engine --list-specs"
"$APP_DIR/Contents/Resources/engine/validator" --list-specs 2>/dev/null | grep -q '"fileType"' \
  && echo "    engine OK" || { echo "    bundled engine failed to list specs" >&2; exit 1; }

if [[ $MAKE_DMG -eq 1 ]]; then
  DMG="$OUT_DIR/csv-validate-$VERSION-macos-$ARCH.dmg"
  echo "==> Creating $DMG"
  rm -f "$DMG"
  hdiutil create -volname "$APP_NAME" -srcfolder "$APP_DIR" -ov -format UDZO "$DMG" >/dev/null
fi

echo "==> Done: $APP_DIR"
