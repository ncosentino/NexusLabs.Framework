# nuget.org Trusted Publishing setup (external manual step)

`.github/workflows/release.yml` uses **nuget.org Trusted Publishing** (OIDC)
instead of a long-lived API key stored as a repo secret. To enable publishing
on the first tag push, complete this one-time setup.

## Prerequisites

- You must own the `NexusLabs.Framework` package on nuget.org (or be a member
  of the organization that owns it).
- You must know your **nuget.org username** (the profile name, NOT your email).
  Find it under your avatar at the top right of nuget.org once logged in.
- The **Trusted Publishing** feature must be available to your account.
  Microsoft is rolling it out gradually. If you don't see the menu item in
  step 2 below, the feature isn't enabled for you yet.

## Step 1: Add a GitHub repo secret for the nuget.org username

The release workflow reads your nuget.org username from a repo secret called
`NUGET_USER`. This keeps the username out of the workflow file.

1. Open <https://github.com/ncosentino/NexusLabs.Framework/settings/secrets/actions>.
2. Click **New repository secret**.
3. Name: `NUGET_USER`
4. Value: your nuget.org profile name (e.g. `ncosentino`). NOT your email.
5. Click **Add secret**.

## Step 2: Add the Trusted Publishing policy on nuget.org

1. Log in to <https://www.nuget.org>.
2. Click your username (top right) and choose **Trusted Publishing** from
   the dropdown menu.
3. The page shows a **Create** form at the top. Fill it in:

   | Field on the page | Value | Notes |
   |---|---|---|
   | **Policy Name** | `NexusLabs.Framework releases (GitHub Actions)` | Free-text, visible only to you. |
   | **Package Owner** | `ncosentino` *(pre-selected; leave it)* | The nuget.org package-owner identity. **The policy authorizes any package owned by this identity** — not just `NexusLabs.Framework`. There is no separate per-package-ID scoping field. |
   | **Repository Owner** | `ncosentino` | Your GitHub username. |
   | **Repository** | `NexusLabs.Framework` | Just the repo name. |
   | **Workflow File** | `release.yml` | **File name ONLY** — the form's own example shows `build.yml`. Do not include the `.github/workflows/` path. |
   | **Environment** *(optional)* | *(leave blank)* | Our workflow does not declare a GitHub Environment. |

4. Click **Create**.

After creation, the Manage section below the Create form will show the new
policy. nuget.org captures the GitHub user ID and repo ID at this point
(visible in the policy details as e.g. `Repository Owner: ncosentino #5169280`).
Those numeric IDs lock the policy to this exact GitHub user + repo and protect
against resurrection attacks (deleting and recreating the repo with the same
name produces a different ID and would not be authorized).

## Step 3 (private repos only): note the 7-day pending window

If `ncosentino/NexusLabs.Framework` is a **private GitHub repo**, the newly
created policy is **temporarily active for 7 days**. During this window, you
must complete at least one successful tag-push publish or the policy goes
inactive and needs to be reset. Public repos do not have this constraint.

You'll see the pending status in the nuget.org Trusted Publishing UI for the
policy.

## How the publish flow works after setup

Tag a release locally and push the tag:

```powershell
git tag -a v0.2.0 -m "0.2.0 - modernization, archived 6 packages"
git push origin v0.2.0
```

That triggers `.github/workflows/release.yml`, which:

1. Builds, tests, and packs the package + symbol package.
2. `NuGet/login@v1` reads `NUGET_USER` from the repo secret, requests a
   short-lived OIDC token from GitHub, and exchanges it with nuget.org for
   a temporary API key (valid ~1 hour).
3. `dotnet nuget push` pushes the package using that temporary key. The
   `--skip-duplicate` flag means re-running the workflow on the same tag
   will not error.

## Emergency manual fallback

If OIDC is unavailable for any reason (incident, account issue, etc.), the
release can be done manually with a long-lived API key:

```powershell
dotnet pack --configuration Release --output ./artifacts
dotnet nuget push ./artifacts/*.nupkg --source https://api.nuget.org/v3/index.json --api-key <long-lived-API-key>
```

A long-lived API key is intentionally NOT configured as a repo secret to avoid
the standing exposure. If you need to use the fallback, fetch a fresh API key
from <https://www.nuget.org/account/apikeys> just before the manual push and
revoke it immediately afterward.

## References

- [Microsoft: Trusted Publishing on nuget.org](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing)
- [NuGet/login@v1 action README](https://github.com/NuGet/login)
- [OpenSSF Trusted Publishers initiative](https://repos.openssf.org/trusted-publishers-for-all-package-repositories)
