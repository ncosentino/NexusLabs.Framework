---
applyTo: "src/**/*.rs,crates/**/src/**/*.rs,apps/**/src/**/*.rs"
scope: "Rust source and security-sensitive boundaries"
---

# Rust unsafe and security-sensitive code

- Prefer safe Rust. Isolate required `unsafe` behind the smallest safe API that can
  maintain the invariant.
- Every unsafe block includes a `// SAFETY:` explanation of the caller, lifetime,
  aliasing, initialization, layout, thread-safety, or foreign-function invariant.
- Every public unsafe function or trait documents its caller obligations under a
  `# Safety` heading.
- Enable or inherit `unsafe_op_in_unsafe_fn` so unsafe operations remain explicit even
  inside an unsafe function.
- Do not introduce unsafe code solely for performance without measurements and focused
  tests that justify the additional proof obligation.
- Validate lengths, counts, recursion depth, indexes, and allocation bounds before
  parsing or allocating from untrusted input.
- Keep secrets, tokens, keys, and private payloads out of `Debug`, logs, panic
  messages, snapshots, and telemetry. Redact before formatting.
- Treat FFI, raw pointers, pinning, custom allocators, cryptographic use, and
  deserialization boundaries as review-sensitive changes.
- Add focused invariant tests; use Miri, fuzzing, or model checking when the failure
  class involves undefined behavior, parsers, or concurrency.
