#!/usr/bin/env bash
set -euo pipefail

# Build the experimental Skia scene project manually.
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

# Invoke dotnet build on the project
exec dotnet build "$PROJECT_DIR/CrystalCatalystSkiaScene.Experimental.csproj" "$@"
