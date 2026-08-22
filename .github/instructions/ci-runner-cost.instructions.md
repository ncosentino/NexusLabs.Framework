---
applyTo: ".github/workflows/*.yml,.github/workflows/*.yaml"
---

# Runner Selection and Cost

This repository is public. Choose `runs-on` on technical merit alone — cost is
not a reason to narrow a matrix, skip an operating system, or leave GitHub's
hosted runners.

## Standard hosted runners are free here

> Use of the standard GitHub-hosted runners is free and unlimited on public
> repositories.
>
> — <https://docs.github.com/en/actions/reference/runners/github-hosted-runners>

That covers every standard label, not only Linux. `ubuntu-latest`,
`windows-latest`, and `macos-latest` all appear in the *Standard GitHub-hosted
runners for public repositories* table, and all cost nothing in this
repository. Adding a Windows job is free.

Public repositories also receive a larger machine than private ones: standard
`ubuntu-latest` and `windows-latest` are 4-CPU / 16 GB here, versus 2-CPU / 8 GB
on private repositories.

## Larger runners are the one billed exception

> Larger runners are always charged for, even when used by public repositories
> or when you have quota available from your plan.
>
> — <https://docs.github.com/en/billing/concepts/product-billing/github-actions>

## Rules

- Use standard `ubuntu-*`, `windows-*`, or `macos-*` labels.
- Never introduce a larger runner (labels such as `ubuntu-latest-4-cores`).
  They are billed even on public repositories.
- Never introduce a self-hosted label. This repository declares no self-hosted
  route, and its `runnerProfiles` contract entry is intentionally empty.
- `windows-latest` currently resolves to `windows-2025`. Pin an explicit image
  only when a job depends on that specific image.

The `pr-*.yml` workflows are upstream component assets. Their `runs-on`
expressions already force hosted runners for a public repository. Do not
hand-edit them; change them upstream instead.
