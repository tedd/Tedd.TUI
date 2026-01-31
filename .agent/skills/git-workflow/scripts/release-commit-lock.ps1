# Release Git Commit Lock Script
#
# Purpose: Releases the git commit lock by removing the lock file.
#          This script should ALWAYS be called after a commit attempt,
#          even if the commit failed, to prevent deadlocks.
#
# Usage: .\.cursor\scripts\release-commit-lock.ps1
#
# Exit codes:
#   0 - Lock released successfully (or was already released)
#
# Lock file location: .git-commit.lock in repository root

param(
    # Lock file path (defaults to .git-commit.lock in current directory)
    [string]$LockFile = ".git-commit.lock"
)

# Remove lock file if it exists (silently ignore if it doesn't)
Remove-Item $LockFile -Force -ErrorAction SilentlyContinue
Write-Host "Commit lock released"

exit 0
