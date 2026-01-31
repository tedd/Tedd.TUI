#!/bin/bash
# Release Git Commit Lock Script
#
# Purpose: Releases the git commit lock by removing the lock file.
#          This script should ALWAYS be called after a commit attempt,
#          even if the commit failed, to prevent deadlocks.
#
# Usage: ./.cursor/scripts/release-commit-lock.sh
#
# Exit codes:
#   0 - Lock released successfully (or was already released)
#
# Lock file location: .git-commit.lock in repository root

# Default lock file path (can be overridden via environment variable)
LOCK_FILE="${LOCK_FILE:-.git-commit.lock}"

# Remove lock file if it exists (ignore errors if it doesn't)
rm -f "$LOCK_FILE"
echo "Commit lock released"

exit 0
