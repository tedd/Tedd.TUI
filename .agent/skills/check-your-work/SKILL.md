---
name: check-your-work
description: Performs a comprehensive review of the current session's work to ensure completeness, correctness, and proper documentation.
---

# Check Your Work

This command performs a comprehensive review of the current session's work.

## When to Use

Run `/check-your-work` at the end of a coding session or before committing changes.

## Workflow

### 1. Extract Goals
Review conversation history. Create a checklist of explicit and implicit goals.

### 2. Verify Each Goal
- Locate implementation.
- Verify completeness and correctness.
- Update checklist status.

### 3. Verification Steps
- `git status` / `git diff`
- Check linter errors.
- Verify logic and edge cases.

### 4. Fix Issues
- Fix syntax/logic errors immediately.
- Ask user about ambiguities.

### 5. Documentation
- Verify Intent Headers and "Why" comments.
- Run `/document` for non-trivial changes.
- Run `/document-module` if detailed DB schema changed.

### 6. Final Report
Present the verified checklist to the user.
