#!/usr/bin/env bash
# Reinstalls the Swap CLI tool locally for testing

set -e

echo "🔄 Reinstalling Swap CLI..."
echo ""

# Get the root directory
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOCAL_FEED="$ROOT_DIR/.nuget/local"

# Uninstall existing CLI
echo "🗑️  Uninstalling existing Swap CLI..."
if dotnet tool uninstall -g Swap.CLI 2>/dev/null; then
    echo "   ✅ Existing CLI uninstalled"
else
    echo "   ℹ️  No existing CLI found"
fi

echo ""

# Pack the CLI
echo "📦 Packing Swap.CLI..."
cd "$ROOT_DIR/tools/Swap.CLI"

# Get version from csproj (simple grep approach)
VERSION=$(grep -oP '<Version>\K[^<]+' Swap.CLI.csproj | head -1)

if [ -z "$VERSION" ]; then
    echo "   ❌ Could not find version in Swap.CLI.csproj"
    exit 1
fi

echo "   Version: $VERSION"

dotnet pack -c Release -o "$LOCAL_FEED"

echo "   ✅ CLI packed successfully"
echo ""

# Install the CLI from local feed
echo "⚙️  Installing Swap CLI from local feed..."
dotnet tool install -g Swap.CLI --add-source "$LOCAL_FEED" --version "$VERSION"

echo "   ✅ CLI installed successfully"
echo ""

# Verify installation
echo "✅ Swap CLI is ready!"
echo ""
echo "📋 Installed version:"
swap --version

echo ""
echo "💡 Try it out:"
echo "   swap new MyTestApp --database sqlite"
echo "   cd MyTestApp"
echo "   dotnet run"
