---
applyTo: "src/lib.rs,crates/**/src/**/*.rs"
scope: "Rust library and public API source"
---

# Rust library and public API design

- Keep items private by default. Prefer `pub(crate)` or a narrow re-export over making
  implementation modules public.
- Public library functions return stable typed errors. Keep `anyhow` at application
  boundaries instead of exposing it as a library API.
- Document public items with rustdoc, including error, panic, and safety behavior.
  Keep examples executable as documentation tests when practical.
- Treat public names, trait bounds, enum variants, feature behavior, and re-exported
  dependency types as semver commitments.
- Use deprecation and a migration path for intentional compatibility changes; do not
  silently repurpose an existing public contract.
- Use `#[non_exhaustive]` only when callers should expect future variants or fields.
  Do not add it mechanically to every public type.
- Avoid leaking an implementation dependency through a public signature unless that
  dependency is deliberately part of the library contract.
- Keep constructors and validation paths responsible for establishing invariants so
  ordinary methods can rely on valid state.
