#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
REPO_ROOT="$(cd "$PROJECT_ROOT/../.." && pwd)"
SOURCE="$REPO_ROOT/assets/external/ambientcg/materials"
TARGET="$PROJECT_ROOT/Assets/External/ambientcg/materials"

if [ ! -d "$SOURCE" ]; then
  echo "Source ambientCG materials not found: $SOURCE" >&2
  exit 1
fi

mkdir -p "$(dirname "$TARGET")"
rm -rf "$TARGET"
cp -R "$SOURCE" "$TARGET"
echo "Copied ambientCG materials to $TARGET"
