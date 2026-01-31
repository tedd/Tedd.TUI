---
name: git-workflow
description: Handles git operations including commit with locking and syncing with remote. Use for committing changes or syncing with origin.
---

# Git Workflow

This skill handles git operations securely using mutex locking for commits to prevent concurrency issues.

## 1. Commit Changes

### Context
- **Locking**: Uses `.git-commit.lock` to ensure exclusive access.
- **Scope**: commits should only include files changed in the current task.

### Instructions

1. **Acquire Lock**:
   ```powershell
   .\.agent\skills\git-workflow\scripts\acquire-commit-lock.ps1
   if ($LASTEXITCODE -ne 0) { throw "Failed to acquire commit lock" }
   ```

2. **Analyze Changes**:
   - Verify status and diffs.
   - Stage ONLY relevant files (don't use `git add .`).

3. **Execute Commit**:
   ```powershell
   # Single Line
   .\.agent\skills\git-workflow\scripts\execute-git-commit.ps1 -Files @("file1", "file2") -Message "Commit message."
   
   # Multi Line
   .\.agent\skills\git-workflow\scripts\execute-git-commit.ps1 -Files @("file1", "file2") -Message "Title" -AdditionalMessages @("Details")
   ```

## 2. Sync with Remote

### Instructions

1. **Check State**: `git status`, `git log`
2. **Pre-Sync**: Ensure clean working tree (commit or stash).
3. **Fetch & Pull**:
   ```powershell
   git fetch origin
   git pull --rebase origin main
   ```
4. **Push**:
   ```powershell
   git push origin main
   ```
5. **Combined (Quick)**:
   ```powershell
   git fetch origin && git pull --rebase origin main && git push origin main
   ```
