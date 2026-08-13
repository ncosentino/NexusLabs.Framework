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

## Testing

The .NET test runner in this project is **Microsoft.Testing.Platform** (MTP),
opted into via `global.json`. A few gotchas worth knowing:

- `dotnet test path/to/sln.slnx` (positional) is **rejected in .NET 10**. Use
  `dotnet test` (no args, picks up the solution in cwd),
  `dotnet test --solution <sln>`, or `dotnet test --project <proj>` instead.
- Under MTP, an empty test project that ships zero tests exits with code **8**
  ("zero tests ran", see <https://aka.ms/testingplatform/exitcodes>).
  `dotnet test` propagates this as a hard failure. If you add a new `*.Tests`
  project, ship at least one passing test in the same change —
  `Test-Templates.ps1` enforces this and CI will fail otherwise.

Report exact warning/error and pass/fail/skip counts rather than saying only
that tests passed. Do not say "all tests pass" — show the numbers.

## Pull Request Delivery

- Local commits are unrestricted checkpoints — commit as often as is useful and
  do not stop to assemble a self-assessment for one. The assessment gate lives
  at ready delivery, not at `git commit`. Local commits belong on feature
  branches. Each clone must activate the repository hook with
  `git config core.hooksPath .githooks` and
  `git config genesis.defaultBranch master`. With that local configuration
  active, `.githooks/pre-push` blocks updates or deletion of `master`; deliver
  changes through pull requests. `.githooks/commit-msg` enforces the
  conventional-commit subject format on every commit.
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
