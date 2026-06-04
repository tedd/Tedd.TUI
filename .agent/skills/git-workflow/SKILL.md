````markdown
---
name: git-workflow
description: Handles git operations including committing changes and syncing with remote. Use for committing changes or syncing with origin.
---

# Git Workflow

This skill handles git operations for committing task-specific changes and syncing with the remote repository.

## 1. Commit Changes

### Context

- **Scope**: Commits should only include files changed in the current task/conversation.
- **Commit operation**: `git add` and `git commit` must be executed in a single line.
- **Do not touch**: Do not touch other files not related to this task/conversation. Do not attempt to revert git or delete files unrelated to this commit.

### Instructions

1. **Review Changes**:
   ```powershell
   git status
   git diff
````

2. **Commit Changes**:

   ```powershell
   git add <files>; git commit -m "<commit message>"
   ```

   Use explicit file paths whenever possible:

   ```powershell
   git add path/to/file1 path/to/file2; git commit -m "<commit message>"
   ```

## 2. Sync with Remote

### Instructions

1. **Check State**:

   ```powershell
   git status
   git log
   ```

2. **Pre-Sync**: Ensure the working tree is clean. Commit or stash local changes before syncing.

3. **Fetch & Pull**:

   ```powershell
   git fetch origin
   git pull --rebase origin main
   ```

4. **Push**:

   ```powershell
   git push origin main
   ```

5. **Combined Quick Sync**:

   ```powershell
   git fetch origin; git pull --rebase origin main; git push origin main
   ```

```
```
