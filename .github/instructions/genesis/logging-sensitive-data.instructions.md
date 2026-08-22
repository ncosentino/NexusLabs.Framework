---
applyTo: "**/*.cs,**/*.go,**/*.rs,**/*.ts,**/*.tsx,**/*.js,**/*.jsx,**/*.dart"
---

# What Is Safe To Log

- Log only bounded, allowlisted metadata such as identifiers, counts, lengths,
  durations, status codes, enum outcomes, content types, model names, token counts,
  latency, and finish reasons.
- Never log credentials, authorization values, cookies, private keys, connection
  strings, query values, full payloads/results/files/messages, prompts, completions,
  embedding input, or tool arguments/results.
- Name the failed validation rule instead of logging the rejected input.
- Metric labels and span names/attributes use bounded vocabularies; never raw
  identifiers, URLs, paths, exception text, or free-form reasons.
- Redact with a fixed marker or a salted correlation hash. Truncation alone is not
  redaction; prefixes still disclose data.
- Payload diagnostics are off by default, require an explicit documented switch and
  debug/trace level, and must remain absent from the default production path.
