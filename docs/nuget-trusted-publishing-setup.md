# nuget.org Trusted Publishing setup (external manual step)

`.github/workflows/release.yml` uses **nuget.org Trusted Publishing** (OIDC)
instead of a long-lived API key stored as a repo secret. To enable publishing
on the first tag push, complete the following one-time setup on nuget.org:

## Steps

1. Log in to <https://www.nuget.org> as the `NexusLabs.Framework` package owner.

2. Navigate to **Account → Trusted Publishers** (or
   `https://www.nuget.org/account/TrustedPublishers`).

3. Click **Add new Trusted Publisher** and choose **GitHub Actions**.

4. Fill in:

   | Field | Value |
   |---|---|
   | Repository owner | `ncosentino` |
   | Repository name  | `NexusLabs.Framework` |
   | Workflow file    | `.github/workflows/release.yml` |
   | Environment      | _(leave blank — the workflow does not use a GitHub Environment)_ |
   | Package owner    | `<your nuget.org user/organization owning the package>` |
   | Package ID glob  | `NexusLabs.Framework` |

5. Save.

After this, any tag push matching `v*.*.*` triggers `release.yml`, which:

- Builds, tests, packs the package and its symbol package (`.snupkg`).
- Calls `NuGet/login@v1` which exchanges the GitHub OIDC token for a
  short-lived nuget.org API key (valid ~1 hour).
- Calls `dotnet nuget push` which picks up the credentials from the
  `NuGet/login@v1` output.

## Emergency manual fallback

If OIDC is unavailable for any reason (incident, account issue, etc.), the
release can be done manually:

```powershell
dotnet pack --configuration Release --output ./artifacts
dotnet nuget push ./artifacts/*.nupkg --source https://api.nuget.org/v3/index.json --api-key <long-lived-API-key>
```

A long-lived API key is intentionally NOT configured as a repo secret to avoid
the standing exposure. If you need to use the fallback, fetch a fresh API key
from nuget.org just before the manual push.

## References

- [Microsoft: Publish a package with Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/publish-a-package#trusted-publisher-setup)
- [NuGet/login@v1 action](https://github.com/NuGet/login)
