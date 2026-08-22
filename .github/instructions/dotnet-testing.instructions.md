---
applyTo: "**/*.Tests/**/*.cs,**/*.Tests/*.csproj,**/tests/**/*.csproj,global.json"
---

# .NET Test Execution

This repository runs tests through **Microsoft.Testing.Platform** (MTP), opted
into by `global.json`. Two MTP behaviors cause failures that look unrelated to
the change being made.

## Invoking the test runner

A positional solution path is rejected on .NET 10:

```
dotnet test path/to/sln.slnx   # rejected
```

Use one of these instead:

```
dotnet test                        # solution in the current directory
dotnet test --solution <solution>
dotnet test --project <project>
```

## A test project must ship at least one test

Under MTP a project that runs zero tests exits with code **8**
(<https://aka.ms/testingplatform/exitcodes>), and `dotnet test` propagates that
as a hard failure. CI fails on the exit code itself — no separate guard script
is involved. When adding a `*.Tests` project, ship at least one passing test in
the same change.

## Reporting results

Report exact warning, error, pass, fail, and skip counts. Do not state that
tests passed without the numbers.
