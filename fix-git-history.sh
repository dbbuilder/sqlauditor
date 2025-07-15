#!/bin/bash
# Remove problematic files from Git history

echo "Removing problematic files from Git history..."

# Remove the files from all commits
git filter-branch --force --index-filter \
  'git rm -r --cached --ignore-unmatch "api/test-publish?" 2>/dev/null || true' \
  --prune-empty --tag-name-filter cat -- --all

echo "Cleanup..."
rm -rf .git/refs/original/
git reflog expire --expire=now --all
git gc --prune=now --aggressive

echo "Done! Now force push to update the remote repository."
echo "Run: git push origin --force --all"