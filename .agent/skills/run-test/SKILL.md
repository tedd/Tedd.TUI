---
name: run-test
description: Runs unit tests for Bifrost and helps diagnose/fix test failures related to recent changes.
---

# Run Test Command

Runs unit tests and analyzes failures.

## Usage

`/run-test [scope]`

- `/run-test` -> All tests
- `/run-test poweroffice` -> Specific module
- `/run-test fix` -> Analyze and fix failures

## Execution

```powershell
cd d:\SourceCode\Amplifai.Bifrost\src\Amplifai.Bifrost.Tests
dotnet test --filter "FullyQualifiedName~Module"
```

## Diagnosis Workflow

1. **Run Tests**: Capture output.
2. **Categorize Error**: Build, Runtime, or Assertion.
3. **Analyze Context**: Check recent changes.
4. **Propose/Apply Fix**:
   - Add missing project refs.
   - Fix using statements.
   - Initialize required props.
   - Update assertions.
