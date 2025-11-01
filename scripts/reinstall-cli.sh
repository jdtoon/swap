#!/usr/bin/env bash
# Reinstalls the Swap CLI tool and framework packages locally for testing

set -euo pipefail

FORCE=false
if [[ "${1-}" == "--force" ]]; then
    FORCE=true
fi

echo "🔄 Reinstalling Swap CLI and Framework Packages..."
echo ""

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
LOCAL_FEED="$ROOT_DIR/.nuget/local"

# --- Clear local feed ---
if [ -d "$LOCAL_FEED" ]; then
    echo "🗑️  Clearing old packages from local feed..."
    rm -f "$LOCAL_FEED"/*.nupkg
    echo "   ✅ Old packages cleared"
else
    mkdir -p "$LOCAL_FEED"
fi
echo ""

# --- Optionally clear NuGet caches ---
if [ "$FORCE" = true ]; then
    echo "🧹 Clearing global NuGet caches..."
    dotnet nuget locals all --clear
    echo "   ✅ NuGet cache cleared"
    echo ""
fi

# --- Uninstall existing CLI ---
echo "🗑️  Uninstalling existing Swap CLI..."
dotnet tool uninstall -g Swap.CLI || true
echo "   ✅ Existing CLI uninstalled"
echo ""

# --- Remove cached packages ---
echo "🧽 Removing any previously installed package versions..."
DOTNET_TOOLS="$HOME/.dotnet/tools"
DOTNET_STORE="$DOTNET_TOOLS/.store"
NUGET_PACKAGES="$HOME/.nuget/packages"
PACKAGES=("swap.cli" "swap.htmx" "swap.patterns" "swap.testing")

for pkg in "${PACKAGES[@]}"; do
    rm -rf "$DOTNET_STORE/$pkg" "$NUGET_PACKAGES/$pkg"
done
rm -f "$DOTNET_TOOLS/swap"
echo "   ✅ Local caches cleaned"
echo ""

# --- Pack framework projects ---
echo "📦 Packing Framework Packages..."
FRAMEWORK_PROJECTS=(
    "framework/Swap.Htmx/Swap.Htmx.csproj"
    "framework/Swap.Patterns/Swap.Patterns.csproj"
    "framework/Swap.Testing/Swap.Testing.csproj"
)

for proj in "${FRAMEWORK_PROJECTS[@]}"; do
    PROJ_PATH="$ROOT_DIR/$proj"
    NAME="$(basename "$proj" .csproj)"
    echo "   📦 Packing $NAME..."
    dotnet pack "$PROJ_PATH" -c Release -o "$LOCAL_FEED" --no-build --nologo
    echo "   ✅ $NAME packed successfully"
done
echo ""

# --- Pack CLI ---
echo "📦 Packing Swap.CLI..."
CLI_DIR="$ROOT_DIR/tools/Swap.CLI"
cd "$CLI_DIR"

VERSION=$(xmllint --xpath "string(/Project/PropertyGroup/Version)" Swap.CLI.csproj)
if [ -z "$VERSION" ]; then
    echo "   ❌ Could not find version in Swap.CLI.csproj"
    exit 1
fi
echo "   Version: $VERSION"

dotnet pack -c Release -o "$LOCAL_FEED" --nologo --no-build
echo "   ✅ CLI packed successfully"
echo ""

# --- Show packages in local feed ---
echo "📋 Packages in local feed:"
ls -1 "$LOCAL_FEED"/*.nupkg | sed 's/^/   • /'
echo ""

# --- Install CLI ---
echo "⚙️  Installing Swap CLI from local feed..."
INSTALL_ARGS=(tool install -g Swap.CLI --add-source "$LOCAL_FEED" --version "$VERSION")
dotnet "${INSTALL_ARGS[@]}" || {
    echo "   🔁 Retrying installation..."
    dotnet tool uninstall -g Swap.CLI || true
    dotnet "${INSTALL_ARGS[@]}"
}

echo "   ✅ CLI installed successfully"
echo ""

# --- Verify installation ---
echo "✅ Swap CLI and Framework Packages are ready!"
echo ""
echo "📋 Installed CLI version:"
swap --version
echo ""
echo "💡 Try it out:"
echo "   swap new MyTestApp --database sqlite --local-nuget"
echo "   cd MyTestApp"
echo "   dotnet run"
echo ""
