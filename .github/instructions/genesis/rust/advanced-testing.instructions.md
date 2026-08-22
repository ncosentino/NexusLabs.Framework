---
applyTo: "tests/**/*.rs,**/tests/**/*.rs,src/**/*.rs,crates/**/src/**/*.rs,fuzz/**,benches/**"
scope: "Rust unit, integration, documentation, fuzz, model, and benchmark tests"
---

# Rust advanced testing

- Keep deterministic logic tests beside the code and repository or process behavior in
  integration tests. Public examples that promise behavior should compile as doc tests.
- Exercise supported feature combinations and workspace members rather than testing
  only the default root build.
- For async code, control time and synchronization explicitly. Prefer paused Tokio time
  or a deterministic signal over sleeps and polling races.
- Use property tests or fuzz targets for parsers, state machines, and untrusted binary
  or text formats. Keep regression inputs for every fixed crash.
- Use Miri for unsafe, aliasing, and undefined-behavior-sensitive code. Use Loom for
  small concurrency primitives whose interleavings cannot be covered reliably by
  ordinary tests. Treat either result as additional evidence, not a proof of soundness.
- Snapshot tests are review artifacts, not automatic truth. Keep snapshots bounded and
  inspect semantic changes before accepting them.
- Benchmarks do not replace correctness tests. Record the workload and compare against
  a meaningful baseline before making a performance claim.
- `unwrap` and `expect` are acceptable in tests when they make setup failure concise;
  production paths must expose contextual errors instead of relying on test conventions.
