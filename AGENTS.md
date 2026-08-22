# Agent Instructions

NexusLabs.Framework is a public .NET library monorepo: foundational building
blocks, Roslyn analyzers and code fixes, strongly typed identifiers, and test
assertion packages published to nuget.org.

## Sources of truth

- The [documentation map](docs/README.md) owns architecture and rationale.
- Path-scoped files under `.github/instructions/` own exact rules for matching
  edits. Managed instructions are defaults; specialize them in separate
  project-owned files rather than editing the managed subtree.
- Code, manifests, schemas, tests, and workflows are executable truth.
  Investigate and correct stale prose when sources disagree.

## Operating safeguards

- Work from evidence. Distinguish verified facts, assumptions, and material
  tradeoffs, and state uncertainty explicitly.
- Do not optimize for agreement. Compare the plausible options and their
  tradeoffs instead of endorsing the first one proposed.
- Do not trust training data for current language, framework, or package
  versions. Verify against the manifests or upstream sources.
- Expected runtime failures return results (`TriedEx<T>`, `TriedNullEx<T?>`),
  never thrown exceptions. Resolve the applicable C# instructions before
  choosing failure semantics.
- Delegate only independent scopes, time-box them, keep synthesis with the
  primary agent, and take over promptly when delegated work stalls.
- Never commit credentials, tokens, live identifiers, or private environment
  values. Before publishing from this public repository, sanitize tracked
  files, commit messages, comments, and pull-request text so they reveal no
  local filesystem context or private repository information.

## Delivery

- `main` is the default branch. Use feature branches and pull requests; local
  commits are unrestricted checkpoints.
- Agent-initiated pull requests default to draft unless a ready pull request is
  explicitly requested.
- [Delivery](docs/delivery.md) owns branch protection, stacks, required gates,
  draft behavior, and the pre-ready assessment.
  [`.github/genesis-delivery.json`](.github/genesis-delivery.json) is the
  machine-readable contract.
- Before delivery, run
  [review-changes](.github/skills/review-changes/SKILL.md).
