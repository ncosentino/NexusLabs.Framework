---
applyTo: "Cargo.toml,Cargo.lock,rust-toolchain,rust-toolchain.toml,crates/**/Cargo.toml,apps/**/Cargo.toml"
scope: "Rust dependency, feature, lockfile, and toolchain policy"
---

# Rust dependencies, features, and MSRV

- Declare shared dependency versions and feature baselines once at the workspace root;
  member crates inherit them instead of drifting independently.
- Keep default features minimal. Disable a dependency's defaults only with evidence
  that the selected feature set is complete and tested.
- Features are additive capabilities, not mutually exclusive build modes. Test the
  supported default, no-default, and all-feature combinations that consumers can use.
- Commit `Cargo.lock` for applications and mixed workspaces. A publishable library may
  omit a package-local lockfile, but it still participates in the repository's locked
  workspace build.
- Declare the minimum supported Rust version when compatibility matters and test it in
  CI before raising it.
- Prefer released registry dependencies. A Git dependency requires a pinned revision,
  a documented reason, and an explicit path back to a release.
- Remove unused direct dependencies and avoid broad feature sets that expand compile
  time or attack surface without a used capability.
- Review dependency updates for MSRV, feature, licensing, supply-chain, and public API
  effects; do not treat a lockfile-only diff as self-explanatory.
