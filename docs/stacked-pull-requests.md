# Stacked pull requests

This repository supports GitHub-native, same-repository stacked pull requests. A stack
is a linear chain of pull requests whose bottom layer targets the default branch and
whose later layers target the branch immediately below them.

The generated delivery contract records the component's classified `PR base` workflow.
`Configure-GitHubDelivery.ps1` keeps the repository's existing delivery-mode decision:

- **Native** uses GitHub's stack-level branch protections, required checks, reviews,
  asynchronous merge API, and merge queue when those controls are available. The
  component adds `merge_group` execution for CI and republishes the PR title, review
  policy, and base contexts required by the queue. Other generated merge-gate
  components also report their required contexts for merge groups.
- **WorkflowRun** uses the trusted private merge workflow to revalidate every ready
  layer against the generated delivery contract before submitting the same
  asynchronous exact-head stack merge.

## Create and maintain a stack

Install GitHub's public-preview CLI extension:

```shell
gh extension install github/gh-stack
```

Create, submit, and rebase stacks with:

```shell
gh stack init
gh stack submit
gh stack rebase
gh stack push
```

Stacks must stay in one repository, remain fully linear, and contain no more than eight
pull requests. Fork pull requests remain ordinary pull requests targeting the default
branch.

## Merge behavior

GitHub evaluates every layer against the stack trunk, so existing CI workflows scoped
to the default branch run for each layer. Squash merging produces one commit per pull
request in bottom-to-top order.

Native GitHub auto-merge is not available for stacked pull requests. Merge a ready
stack from the GitHub merge box, `gh stack`, or a configured merge queue. In
WorkflowRun mode, the trusted controller supplies equivalent automatic progression
after every generated required check succeeds on the exact layer heads.

## Configure delivery

Plan the live repository configuration before applying it:

```powershell
./scripts/delivery/Configure-GitHubDelivery.ps1 -DeliveryMode Auto
```

Public repositories normally select Native mode. Private repositories whose plan
cannot enforce branch protection select WorkflowRun mode and require the explicit
`-AllowUnprotectedPrivate` acknowledgement when applying.

The GitHub stack APIs are in public preview and require API version `2026-03-10`.
Unsupported or malformed stack metadata fails closed rather than being treated as an
ordinary non-default-base pull request.
