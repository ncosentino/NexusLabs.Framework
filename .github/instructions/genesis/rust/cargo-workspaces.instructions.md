---
applyTo: "Cargo.toml,crates/**/Cargo.toml,apps/**/Cargo.toml"
scope: "Rust workspace and crate manifests"
---

# Rust workspace and crate boundaries

- Keep the root manifest authoritative for workspace members, resolver, shared package
  metadata, dependency versions, lints, and host-owned release profiles.
- In member manifests, inherit shared values with `workspace = true`; add a local
  version, lint, or profile only when the crate genuinely requires a different
  contract.
- Give each crate one coherent responsibility. Put reusable policy and domain logic in
  libraries and keep process startup, transport, and configuration at binary edges.
- Keep dependency direction explicit and acyclic. Shared crates must not depend on the
  applications that compose them.
- Use resolver version 3 for edition-2024 workspaces so target and feature resolution
  follow the current Cargo contract.
- Do not assemble manifests by text concatenation. Make structural edits and fail on
  conflicting members, dependencies, features, lints, or profiles.
- Treat root workspace policy as an API for every member: test representative members
  independently as well as through `--workspace`.
