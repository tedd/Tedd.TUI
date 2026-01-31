---
description: Adds or updates entries in the customer-facing changelog (docs/Changelog.md).
---
# Add Changelog Entry

This command adds or updates entries in the customer-facing changelog (`docs/Changelog.md`).

## When to Use

Run `/add-changelog` when you've completed a change that is notable for end-users:

- New user-facing features
- Bug fixes that affected users
- Significant performance improvements
- UI/UX improvements

**Do NOT use for:**
- Internal refactoring
- Code cleanup
- Developer tooling changes
- Backend-only changes invisible to users

## Related Commands

| Command | Audience | Use When |
|---------|----------|----------|
| `/add-changelog` | End users | User-facing features, fixes, improvements |
| `/document` | Developers | Technical documentation for any significant change |

**Tip:** A single change may warrant entries in multiple changelogs. For example, a new module might need:
- `/add-changelog` — user-facing feature description
- `/document` — technical implementation details

## Workflow

### 1. Read Current Changelog

First, read the existing changelog to understand the format and check for existing coverage:

```
Read: docs/Changelog.md
```

**Check before adding:**
- Is this feature already mentioned in a recent entry?
- Could this be merged with an existing entry?
- Would updating an existing entry be clearer than adding a new one?

### 2. Determine Entry Type

Choose the appropriate prefix:
- **Added** - New feature
- **Fixed** - Bug fix  
- **Improved** - Enhancement to existing feature
- **Changed** - Behavior change

### 3. Add or Update Entry

Add a new entry at the **top** of the current year's section, OR update/merge with a recent entry if appropriate.

**When to merge entries:**
- Multiple related changes on the same day → combine into one entry
- Consecutive changes to the same feature → update the existing entry
- Follow-up fixes to a recently added feature → merge into the original entry

**Format:**
```markdown
- **YYYY-MM-DD** - <Type>: Brief one-liner describing the change
```

**Examples:**
```markdown
- **2026-01-15** - Added: Real-time session logging with live updates
- **2026-01-14** - Fixed: Dashboard charts not loading for large datasets
- **2026-01-12** - Improved: Report generation performance by 40%
- **2026-01-10** - Changed: Default date range filter now uses current fiscal year
```

### 4. Create New Year Section (If Needed)

If adding the first entry for a new year, create a new section **above** the previous year:

```markdown
## 2027

- **2027-01-02** - Added: New feature description

---

## 2026

- **2026-12-31** - Previous entries...
```

## Entry Guidelines

### Good Entries

✅ Describe the **user benefit**, not the technical implementation:
- "Added: Export reports to Excel format"
- "Fixed: Login timeout no longer logs out users during active work"
- "Improved: Search results now load instantly"

### Bad Entries

❌ Avoid technical jargon users won't understand:
- "Refactored DbContext to use split queries"
- "Updated Serilog configuration"
- "Fixed null reference in UserService.cs"

### Keep It Brief

One line, ideally under 80 characters. If you need more detail, the full documentation is in `docs/YYYY/MM/`.

## Example Execution

```
# User asks to add changelog entry for new dashboard feature:

1. Read: docs/Changelog.md
2. Identify today's date and feature type
3. Add entry at top of current year section:
   - **2026-01-15** - Added: Interactive dashboard with customizable widgets
4. Save file
```

## Verification

After adding or updating:
- [ ] Entry is at the top of the correct year section (or merged with recent entry)
- [ ] Date format is correct (YYYY-MM-DD)
- [ ] Description is user-friendly (no technical jargon)
- [ ] Entry is a single line
- [ ] Consider: Does this also warrant an investor changelog entry? → `/add-investor-changelog`
