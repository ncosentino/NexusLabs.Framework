---
applyTo: "src/**/*.rs,crates/**/src/**/*.rs,apps/**/src/**/*.rs,migrations/**"
scope: "Rust serialization, protocol, parser, and persistence boundaries"
---

# Rust serialization, protocols, and persistence

- Separate wire, storage, and domain types when their compatibility or validation
  rules differ. Conversion is the boundary where invariants are checked.
- Version durable or externally exchanged formats explicitly. Preserve readers for
  supported prior versions or provide a deliberate migration.
- Bound message size, collection length, nesting, decompression, and allocation before
  materializing untrusted data.
- Validate required fields and authorization-relevant values before side effects. Do
  not use permissive defaults that turn malformed security fields into valid input.
- Keep protocol parsing deterministic and independent from network or storage effects
  so malformed inputs can be fuzzed and regression-tested directly.
- Use parameterized database operations and explicit transaction boundaries. Keep a
  transaction short and never hold it across unrelated network calls.
- Treat committed migrations as append-only history. Test migration from the oldest
  supported schema and verify failure leaves the database recoverable.
- Never serialize or persist secrets merely because a type derives a general-purpose
  serialization or debugging trait.
