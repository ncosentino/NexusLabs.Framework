---
applyTo: "**/*{Stream,Serializer,Transport,Reader,Writer}*.cs"
---

# Stream Capability Rules

- Treat `Stream` as forward-only unless its contract guarantees seekability.
- Before using `Seek`, `Position`, or `Length`, require documented seekability or
  guard with `CanSeek`.
- For backward access on a non-seekable source, use bounded read-ahead rather than
  copying an unbounded source into memory.
