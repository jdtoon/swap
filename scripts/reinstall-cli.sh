#!/usr/bin/env bash
# Reinstalls the Swap CLI tool and framework packages locally for testing

set -e

echo "🔄 Reinstalling Swap CLI and Framework Packages..."
echo ""

# Get the root directory
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOCAL_FEED="$ROOT_DIR/nuget/local"

# Create local feed directory if it doesn't exist, or clear it
if [ -d "$LOCAL_FEED" ]; then
    echo "🗑️  Clearing old packages from local feed..."
    rm -f "$LOCAL_FEED"/*.nupkg
    echo "   ✅ Old packages cleared"
    echo ""
else
    mkdir -p "$LOCAL_FEED"
fi

# Uninstall existing CLI
echo "🗑️  Uninstalling existing Swap CLI..."
if dotnet tool uninstall -g Swap.CLI 2>/dev/null; then
    echo "   ✅ Existing CLI uninstalled"
else
    echo "   ℹ️  No existing CLI found"
fi

echo ""

# Pack all framework packages
echo "📦 Packing Framework Packages..."

FRAMEWORK_PROJECTS=(
    "framework/Swap.Htmx/Swap.Htmx.csproj"
    "framework/Swap.Patterns/Swap.Patterns.csproj"
    "framework/Swap.Testing/Swap.Testing.csproj"
)

for PROJECT in "${FRAMEWORK_PROJECTS[@]}"; do
    PROJECT_PATH="$ROOT_DIR/$PROJECT"
    PROJECT_NAME=$(basename "$PROJECT" .csproj)
    
    echo "   📦 Packing $PROJECT_NAME..."
    
    dotnet pack "$PROJECT_PATH" -c Release -o "$LOCAL_FEED" --nologo
    
    echo "   ✅ $PROJECT_NAME packed successfully"
done

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

dotnet pack -c Release -o "$LOCAL_FEED" --nologo

echo "   ✅ CLI packed successfully"
echo ""

# List all packages in local feed
echo "📋 Packages in local feed:"
ls -1 "$LOCAL_FEED"/*.nupkg | xargs -n 1 basename | sed 's/^/   • /'
echo ""

# Install the CLI from local feed
echo "⚙️  Installing Swap CLI from local feed..."
dotnet tool install -g Swap.CLI --add-source "$LOCAL_FEED" --version "$VERSION"

echo "   ✅ CLI installed successfully"
echo ""

# Verify installation
echo "✅ Swap CLI and Framework Packages are ready!"
echo ""
echo "📋 Installed CLI version:"
swap --version

echo ""
echo "💡 Try it out:"
echo "   swap new MyTestApp --database sqlite --local-nuget"
echo "   cd MyTestApp"
echo "   dotnet run"
