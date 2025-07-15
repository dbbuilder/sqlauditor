#!/bin/bash
# Clean build artifacts and temporary files

echo "Cleaning build artifacts..."

# Remove all publish directories
find . -type d -name "publish" -exec rm -rf {} + 2>/dev/null || true
find . -type d -name "test-publish" -exec rm -rf {} + 2>/dev/null || true
find . -type d -name "win-publish" -exec rm -rf {} + 2>/dev/null || true
find . -type d -name "*publish?" -exec rm -rf {} + 2>/dev/null || true

# Remove bin and obj directories
find . -type d -name "bin" -exec rm -rf {} + 2>/dev/null || true
find . -type d -name "obj" -exec rm -rf {} + 2>/dev/null || true

# Remove any files with invalid characters
find . -name "*\?*" -delete 2>/dev/null || true

echo "Clean complete!"