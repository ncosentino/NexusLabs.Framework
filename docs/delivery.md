# Delivery

The machine-readable contract in
[`.github/genesis-delivery.json`](../.github/genesis-delivery.json) is the source
of truth for required checks, draft behavior, and workflow roles. This page
explains the model; it never overrides that file.

## Branches and topology

`main` is the default branch. Every pull request targets it unless it is a layer
in a stacked pull request. Stacks are enabled through the
`github-stacked-pr-delivery` component — see
[stacked pull requests](stacked-pull-requests.md) for the rules. A stack is
linear, stays in this repository, is at most eight layers, and its bottom layer
targets `main`. Fork pull requests are always ordinary pull requests targeting
`main`.

Local commits are unrestricted checkpoints. Commit as often as is useful; the
assessment gate is at ready delivery, not at `git commit`. Local commits belong
on feature branches.

Each clone must activate the repository hooks:

```
git config core.hooksPath .githooks
git config genesis.defaultBranch main
```

With that active, `.githooks/pre-push` blocks updates or deletion of `main`, and
`.githooks/commit-msg` enforces the conventional-commit subject format.

## Draft and ready

"Open a PR" and "publish a PR" mean ready for review. "Open a draft PR" and
"open a PR so I can review" mean draft. Agent-initiated pull requests default to
draft unless a ready pull request is explicitly requested.

Draft pull requests publish `Draft CI` and skip the full build/test/package job.
Moving a pull request to ready starts fresh full validation and publishes the
stable required `CI` check.

## Merge gates

The required gates are `CI`, `PR base`, `PR title`, and `Review policy`.

`PR base` is deliberately the only pull-request workflow with no branch filter,
so a pull request targeting neither `main` nor a valid stack layer fails visibly
instead of silently receiving no checks at all.

`enforce_admins` is enabled on `main`. There is no administrator bypass: a
blocked pull request is fixed by making its required checks pass, not by
overriding protection.

When `GENESIS_REVIEW_POLICY=copilot-one-approval`, a ready Copilot-authored pull
request requires one OWNER, MEMBER, or COLLABORATOR approval on its current head
SHA. A later Copilot push invalidates the prior approval.

In this public repository, workflows from every external fork require explicit
maintainer approval before execution. Approval authorizes the entire proposed
workflow, including runner selection; the current CI routes all pull requests to
GitHub-hosted runners.

Pull request titles use conventional commit semantics and stay at most 72
characters. Squash merging uses the pull request title as the default-branch
commit subject and the body as the commit message.

## Before marking a pull request ready

1. Confirm the title follows the required conventional format.
2. Record validation evidence, and assess omitted behavior, implementation gaps,
   failing or missing tests, technical debt, missing coverage, weak assertions,
   and assumptions.
3. Fix every high-severity issue or keep the pull request in draft. Disclose
   remaining medium- and low-severity findings in the body.
