---
name: create-test
description: Creates test cases for the specified component, or from context of recent changes.
---

# Create Test Command

Creates test cases for the specified component.

## Usage

`/create-test [scope]`

- `create-test cas` -> Test Module and DbContext
- `create-test dal masteraccountmapping` -> Test DAL class
- `create-test` -> Infer from context

## Workflow

1. **Identify Scope**: Determine usage (DbContext, DAL, etc.).
2. **Add Comments**: Add `/// <remarks> Tested in: ...` comments to source code.
3. **Create Test File**: In `src/Amplifai.Bifrost.Tests/`.
4. **Follow Patterns**: Use `TestDatabaseFixture`, `TestDatabaseFactory`.

## Test Patterns

### DbContext Tests
- Verify migrations/database creation.
- Verify all DbSets are queryable.
- Verify inserts/updates/constraints.

### DAL Tests
- Verify all CRUD operations.
- Use `TestDatabaseFactory.CreateForDal()`.
