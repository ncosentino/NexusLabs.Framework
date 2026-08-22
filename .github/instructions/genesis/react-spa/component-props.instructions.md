---
applyTo: '**/*.tsx'
---

# React component props

- Every `Props`/`*Props` field is `readonly`; array contracts use
  `ReadonlyArray<T>` rather than `T[]`.
- Production props contain only values a real caller supplies.
- Do not add test-only controls such as `now`, `forceLoading`, test fetch functions, or
  `__*ForTests` seams. Inject controllable behavior through context providers,
  factories, or DI.
- Component-local mutation is allowed; the external prop contract remains immutable.
