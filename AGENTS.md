# Agent Instructions

## Behavior

- Be unbiased. Do not optimize for agreement.
- When weighing options, always do a pros/cons analysis.
- Always compare the main plausible paths and explain tradeoffs.
- Do not blindly agree with the user; compare and contrast alternatives fairly.
- State uncertainty explicitly.
- Distinguish verified facts from assumptions.

### Coding Behavior

- Do NOT rely on your training data for latest language and tech stack versions. Research with web searches.
- Back important claims with concrete evidence from code, tests, outputs, docs, or measurements.

### Research Behavior

- Run multiple parallel sub agents to collect data.
- Analyze the results to form a consensus to present to the user.
- Back up any claims with concrete evidence and citations.

## Project Overview

<!-- Describe what this project does, what problem it solves, and who it serves. -->

## Architecture

<!-- Key patterns, frameworks, and structural decisions:
     - Framework choices (e.g., ASP.NET Core, Carter, MassTransit)
     - DI and registration approach
     - Key data stores and access patterns
     - External integrations -->

## Conventions

<!-- Naming, structure, and coding conventions specific to this project.
     Include anything an agent needs to know that isn't obvious from the code. -->
 - Never throw exceptions unless you intend for an application to terminate on the spot.
   - Throwing is reserved for unrecoverable programming errors (e.g., invalid configuration at startup), not runtime failures.
   - Instead, use a result pattern so that you can return successful state, or otherwise, an error.
   - The standard result types are `TriedEx<T>`, `TriedNullEx<T?>`, and `Exception?`, produced via `Try.GetAsync`/`Try.Get` helpers.
     - These are available in `NexusLabs.Framework`, or your codebase may have its own
   - Exception objects are acceptable error result types when not crossing a serialization boundary.
   - Validation failures are not exceptions — use result-based validation patterns.
## Commit Workflow

Before every `git commit`, you MUST complete this procedure. Do not skip steps.

### Step 1: Build and test

Run the build and tests. Record the exact output — warning count, error count, tests passed/failed/skipped.

The .NET test runner in this project is **Microsoft.Testing.Platform** (MTP), opted into via `global.json`. A few gotchas worth knowing:

- `dotnet test path/to/sln.slnx` (positional) is **rejected in .NET 10**. Use `dotnet test` (no args, picks up the solution in cwd), `dotnet test --solution <sln>`, or `dotnet test --project <proj>` instead.
- Under MTP, an empty test project that ships zero tests exits with code **8** ("zero tests ran", see <https://aka.ms/testingplatform/exitcodes>). `dotnet test` propagates this as a hard failure. If you add a new `*.Tests` project, ship at least one passing test in the same change — `Test-Templates.ps1` enforces this and CI will fail otherwise.

### Step 2: Self-assessment

Evaluate your work against every item below. For each one, write a honest one-line assessment:

| Priority | Item | Your assessment |
|----------|------|-----------------|
| HIGH | Omitted behavior that was discussed or you realize upon reflection | |
| HIGH | Implementation gaps | |
| HIGH | Test results — did you run them and what were the numbers | |
| MEDIUM | Tech debt introduced | |
| MEDIUM | Missing test coverage | |
| MEDIUM | Weak assertions in tests | |
| LOW | Assumptions you made | |

### Step 3: Present to the user

Share the completed table with the user.

- Any HIGH issue that is not "none" → Do NOT commit. Share with the user but do not wait for review. You MUST fix it before your next commit attempt.
- Any MEDIUM issue that is not "none" → Do NOT commit. Share with the user and the user must acknowledge it before you proceed.
- Only LOW issues or all "none" → Share with the user. Proceed to commit without waiting for approval.

### Step 4: Commit

Only after the user has reviewed your self-assessment and approved, set the acknowledgment and commit:

```powershell
$env:GENESIS_PRECOMMIT_ACK = "true"
git commit -m "type: description"
```

The pre-commit hook will block you if you skip step 3. This is intentional.

### Step 5: Share evidence

After the commit succeeds, share with the user:
- Test results: exact pass/fail/skip counts
- What specific behavior the tests verified
- Build output: warning and error counts
- Files changed summary

Do not say "all tests pass" — show the numbers.

## Pull Request Delivery

- Work and local checkpoint commits belong on feature branches. The existing
  commit assessment and acknowledgment workflow above remains mandatory. Each
  clone must activate the repository hook with
  `git config core.hooksPath .githooks` and
  `git config genesis.defaultBranch master`. With that local configuration
  active, `.githooks/pre-push` blocks updates or deletion of `master`; deliver
  changes through pull requests.
- Run targeted checks while iterating and the full repository validation before
  delivery. This public repository uses GitHub-hosted runners; no self-hosted
  runner route is declared in `.github/genesis-delivery.json`.
- "Open a PR" and "publish a PR" mean ready for review. "Open a draft PR" and
  "open a PR so I can review" mean draft. Agent-initiated PRs default to draft
  unless the user explicitly requests a ready PR.
- Draft pull requests publish `Draft CI` without the full build/test/package
  job. Moving a pull request to ready starts fresh full validation and publishes
  the stable required `CI` check.
- When `GENESIS_REVIEW_POLICY=copilot-one-approval`, a ready Copilot-authored
  pull request requires one OWNER, MEMBER, or COLLABORATOR approval on its
  current head SHA. A later Copilot push invalidates the prior approval.
- In this public repository, workflows from every external fork require
  explicit maintainer approval before execution. Approval authorizes the entire
  proposed workflow, including runner selection; the current CI routes all
  pull requests to GitHub-hosted runners.
- Pull request titles must use conventional commit semantics and remain at most
  72 characters. Squash merging uses the PR title as the default-branch commit
  subject and the PR body as the commit message.
- After the migration is merged and remote activation is separately approved,
  protected native auto-merge is the delivery lane. Do not apply or weaken
  repository settings from a migration branch.

Before opening a ready PR, publishing a draft, or pushing more commits to an
already-ready PR:

1. Confirm the PR title follows the required conventional format.
2. Record validation evidence and assess omitted behavior, implementation gaps,
   failing or missing tests, technical debt, missing coverage, weak assertions,
   and assumptions.
3. Fix every high-severity issue or keep the PR in draft. Disclose remaining
   medium- and low-severity findings in the PR body.

## Out of Scope

<!-- What should NOT be changed or touched without explicit approval. -->
