---
applyTo: "**/proxy.{js,ts},**/app/**/route.{js,ts},**/app/**/*.{js,jsx,ts,tsx}"
scope: "Next.js Route Handlers, Server Actions, and proxy boundaries"
---

# Next.js authorization boundaries

- Proxy or middleware is never the sole authentication or authorization boundary.
- Protected handlers/actions require downstream authentication, a provider-free principal,
  application authorization before effects, and explicit classification; public entries require justification.
- Browser mutations evaluate exact Origin, Fetch Metadata, content type, session, session-bound CSRF, and product authorization in that order.
- Missing browser signals never widen access; non-browser callers use a separate credentialed kind.
- Derive the route/access inventory from registered code and the production framework surface; never maintain a second protected-path list.
- Keep 401, disclosed 403, and no-oracle 404 stable and sanitized; the RFC 9110 existence-hiding allowance is section 15.5.5.
- Pair crafted proxy/matcher bypass negative tests proving no protected operation was invoked with authorized positive controls.
