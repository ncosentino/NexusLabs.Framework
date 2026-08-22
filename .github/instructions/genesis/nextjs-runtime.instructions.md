---
applyTo: "**/next.config.{js,mjs,ts},**/proxy.{js,ts},**/app/**/*.{js,jsx,ts,tsx}"
scope: "Next.js App Router configuration and runtime code"
---

# Next.js runtime boundaries

## Installed-version authority

- Install dependencies and read the relevant guide under `node_modules/next/dist/docs/`
  before framework changes; it matches the installed `next` version.
- Keep `agentRules` enabled. `next dev` may add or refresh the `<!-- BEGIN:nextjs-agent-rules -->`
  block; Next.js owns only that marker-bounded block. Preserve surrounding project
  guidance and commit the one-time managed update.
- Use official `vercel/next.js` skills: `next-dev-loop`, `next-cache-components-adoption`,
  `next-cache-components-optimizer`, and `next-partial-prefetching-adoption`. Install with
  `npx skills add vercel/next.js --skill <name>`; never use `vercel-labs/next-skills`.

## Runtime contract

- Read `next.config.*` first. `output: 'export'` forbids Route Handlers, Server Actions,
  request-time rendering, and authenticated server integrations.
- Without static export, pages/layouts are Server Components by default and Route
  Handlers use the selected server runtime.
- Add `'use client'` only for state, events, effects, context, or browser APIs; never
  import `server-only` code into a Client Component.
- Keep non-`NEXT_PUBLIC_` values behind one validated server boundary; public values
  are inlined into browser bundles.
- Treat `params` and `searchParams` as async in current App Router entry points.
- Route Handlers use Web `Request`/`Response`, declare material runtime/caching behavior,
  and have direct deterministic tests when no live provider is required.
