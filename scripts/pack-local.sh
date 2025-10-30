#!/usr/bin/env bash
# Pack all Swap framework packages and publish to local NuGet feed
# Run this after making changes to framework packages during development

set -e

echo "🔨 Building and packing Swap framework packages..."

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOCAL_FEED="$ROOT_DIR/.nuget/local"

# Ensure local feed directory exists
mkdir -p "$LOCAL_FEED"

# Clean old packages from local feed (optional - keeps only latest)
echo "🧹 Cleaning old packages from local feed..."
rm -f "$LOCAL_FEED"/*.nupkg

# Pack Swap.Htmx
echo ""
echo "📦 Packing Swap.Htmx..."
cd "$ROOT_DIR/framework/Swap.Htmx"
dotnet pack -c Release -o "$LOCAL_FEED"

# Pack Swap.Patterns
echo ""
echo "📦 Packing Swap.Patterns..."
cd "$ROOT_DIR/framework/Swap.Patterns"
dotnet pack -c Release -o "$LOCAL_FEED"

# Pack Swap.Testing
echo ""
echo "📦 Packing Swap.Testing..."
cd "$ROOT_DIR/framework/Swap.Testing"
dotnet pack -c Release -o "$LOCAL_FEED"

# Pack Swap.CLI
echo ""
echo "📦 Packing Swap.CLI..."
cd "$ROOT_DIR/tools/Swap.CLI"
dotnet pack -c Release -o "$LOCAL_FEED"

cd "$ROOT_DIR"

echo ""
echo "✅ All packages packed successfully!"
echo ""
echo "📦 Local packages available:"
ls -1 "$LOCAL_FEED"/*.nupkg | xargs -n1 basename | sed 's/^/   - /'

echo ""
echo "💡 Tip: Projects using nuget.config will automatically use these local packages"
