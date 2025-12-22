#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="$SCRIPT_DIR/build"
PACKAGE_DIR="$BUILD_DIR/csharp-project-expert"
SRC_DIR="$SCRIPT_DIR/src/CSharpExpertCli"
VERSION="${1:-1.0.0}"

echo "📦 Packaging C# Project Expert Skill v${VERSION}..."

# 1. Clean previous build
echo "🧹 Cleaning previous build artifacts..."
rm -rf "$BUILD_DIR"
mkdir -p "$PACKAGE_DIR"/{scripts,references}

# 2. Build Release
echo "🔨 Building Release configuration..."
cd "$SRC_DIR"
dotnet build -c Release --nologo

# 3. Copy executable + all dependencies
echo "📋 Copying executable and dependencies..."
cp -r "$SRC_DIR/bin/Release/net10.0"/* "$PACKAGE_DIR/scripts/"
chmod +x "$PACKAGE_DIR/scripts/CSharpExpertCli"

# 4. Copy documentation
echo "📚 Copying documentation files..."
cp "$SCRIPT_DIR/SKILL.md" "$PACKAGE_DIR/"
cp "$SCRIPT_DIR/references"/*.md "$PACKAGE_DIR/references/"

# 5. Create ZIP
echo "🗜️  Creating ZIP archive..."
cd "$BUILD_DIR"
zip -r "csharp-project-expert-v${VERSION}.zip" csharp-project-expert/ > /dev/null

# 6. Validate package
echo "✅ Validating package..."
if "$PACKAGE_DIR/scripts/CSharpExpertCli" --help > /dev/null 2>&1; then
    echo "✅ Executable validation passed"
else
    echo "❌ Executable validation failed"
    exit 1
fi

# Check SKILL.md exists and has YAML frontmatter
if head -n 1 "$PACKAGE_DIR/SKILL.md" | grep -q "^---$"; then
    echo "✅ SKILL.md YAML frontmatter found"
else
    echo "❌ SKILL.md YAML frontmatter validation failed"
    exit 1
fi

# Count files
FILE_COUNT=$(find "$PACKAGE_DIR" -type f | wc -l | tr -d ' ')
echo "📊 Package contains $FILE_COUNT files"

# Show package size
PACKAGE_SIZE=$(du -h "$BUILD_DIR/csharp-project-expert-v${VERSION}.zip" | cut -f1)
echo "📦 Package size: $PACKAGE_SIZE"

echo ""
echo "✅ Package created successfully!"
echo "📍 Location: $BUILD_DIR/csharp-project-expert-v${VERSION}.zip"
echo ""
echo "To test the package:"
echo "  1. Extract: unzip build/csharp-project-expert-v${VERSION}.zip -d /tmp/"
echo "  2. Run: /tmp/csharp-project-expert/scripts/CSharpExpertCli --help"
