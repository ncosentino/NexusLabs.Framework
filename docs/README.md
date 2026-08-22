# Documentation

Canonical map of the documentation in this repository. Every maintained page is
reachable from here.

Executable truth lives outside these pages: `.github/genesis-delivery.json`
declares the delivery contract, `.github/instructions/` owns exact rules for
matching edits, and the solution, projects, and workflows own build and test
behavior. When prose and those sources disagree, the sources win.

## Reference

- [Analyzer rules](analyzers/README.md) — every `NLF`, `NLS`, and `NLT`
  diagnostic shipped by the analyzer packages, with rationale and examples.

## Delivery

- [Stacked pull requests](stacked-pull-requests.md) — when a stack is allowed,
  how layers are based, and the limits enforced by the `PR base` gate.
- [NuGet Trusted Publishing setup](nuget-trusted-publishing-setup.md) — the
  OIDC configuration used by the release workflow to publish to nuget.org.

## Release history

- [v0.2 breaking changes](v0.2-breaking-changes.md) — migration notes for the
  v0.2 release.
- [Archived packages](archived-packages/README.md) — packages that are no
  longer published, and what replaced them.

Release-by-release detail lives in [CHANGELOG.md](../CHANGELOG.md).
