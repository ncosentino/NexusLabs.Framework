---
applyTo: "src/**/*.rs,crates/**/src/**/*.rs,apps/**/src/**/*.rs"
scope: "Rust logging, tracing, metrics, and protocol output"
---

# Rust structured observability

- CLI applications keep machine-readable output on stdout and diagnostics on stderr.
  Long-running services may use `tracing` for structured events and spans.
- Emit stable field names and bounded identifiers. Do not record message bodies,
  credentials, tokens, keys, raw user content, or unbounded payloads by default.
- Record lifecycle and boundary events that explain state transitions; do not
  instrument every function or duplicate the same error at multiple layers.
- Attach context as an error propagates, then report it once at the owning process or
  request boundary.
- Keep library crates subscriber-neutral. The final binary chooses formatting,
  filtering, exporters, and destinations.
- Reserve stdout when a protocol such as MCP stdio owns it. Logs, panics, and
  diagnostics must not corrupt the transport stream.
- Make telemetry export optional and failure-bounded. An unavailable collector must
  not silently break the service's primary behavior.
- Hash, classify, or redact sensitive identifiers before correlation; document when a
  field is intentionally safe to export.
