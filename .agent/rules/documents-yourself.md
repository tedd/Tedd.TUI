---
trigger: always_on
description: Capture intent and leave high-signal breadcrumbs for future self (Bifrost)
globs: **/Amplifai.Bifrost*/**/*.cs, **/Amplifai.Brightline*/**/*.cs, **/Corvenia.AccountingStandards*/**/*.cs
---

# Overview

Leave durable context for non-trivial changes: **what you intended**, **why**, **constraints**, and **how to verify**. Prefer short, structured notes that survive refactors.

Apply when:

- Changing architecture, boundaries, or responsibilities
- Touching cross-cutting concerns (auth, caching, concurrency, serialization, migrations, config)
- Adding non-obvious logic, invariants, or performance-sensitive code
- Fixing subtle bugs
- Introducing feature flags, shims, or compatibility layers
- **Database schema changes** (PK/FK/Index) - See module documentation requirements below

Skip when:

- Mechanical refactors with no behavior change
- Self-explanatory glue
- Formatting-only edits

## What to produce

### 1) Intent header (near the change)

Add a short comment block at file top or above the main unit:

- **Intent:** outcome in one sentence
- **Why:** 1–3 bullets (include key tradeoffs)
- **Constraints/Invariants:** what must remain true
- **Failure modes:** how it breaks and symptoms
- **Verification:** how to test and where to observe
- **Refs:** tickets/ADRs/docs (if any)

### 2) Inline "Why" comments (sparingly)

Explain **why**, not **what**. If the comment restates code, refactor instead.

Use for:

- Ordering, edge cases, invariants
- Concurrency, retries, idempotency, caching
- Protocol/version assumptions
- Performance thresholds

Avoid narration or long prose.

### 3) Agent note (for multi-step work)

If part of a broader effort, add a brief note in one stable place:

- `docs/agent-notes/<area>.md`, or
- module `README.md`, or
- `ADR/`

Include:

- Current direction + next 1–3 steps
- Open questions
- What must not break
- First place to look when it fails

## Templates

### Intent header

```text
// Intent: <what this enables/fixes>
// Why:
// - <primary reason>
// - <tradeoff/constraint>
// Constraints/Invariants:
// - <must remain true>
// Failure modes:
// - <symptoms>
// Verification:
// - <tests/steps/logs>
// Refs: <ticket/ADR/doc>
```

### Inline "Why"

```text
// Why: <non-obvious reason>; <what breaks if changed>
```

### Bug fix

```text
// Bug: <one-line>
// Root cause: <why>
// Fix: <principle>
// Regression: <test/coverage>
```

## Changelog Updates

After completing non-trivial changes, consider updating changelogs:

| Changelog | Audience | Trigger |
|-----------|----------|---------|
| `docs/Changelog.md` | End users | User-facing features, fixes, improvements |

**Commands:**
- `/add-changelog` — Add/update customer-facing changelog

**Editing and Merging:**
Changelogs are not append-only. Merge related changes, update recent entries, and consolidate consecutive small changes into single descriptions for clarity.

## Quality bar
- Every non-trivial change has an intent note
- Comments describe intent/invariants, not narration
- Verification is explicit and actionable
- Temporary behavior is labeled with removal criteria
- User-facing changes update Yggdrasil `docs/Changelog.md`