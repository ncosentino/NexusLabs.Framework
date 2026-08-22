---
applyTo: "**/{vite,vitest}.config.{ts,mts,cts,js,mjs,cjs}"
---

# Vitest Coverage Rules

These rules apply to any Vitest configuration, whether the config lives in its
own `vitest.config.*` or is combined into a `vite.config.*` via the `test` key.
Vitest resolves `.ts`, `.mts`, `.cts`, `.js`, `.mjs`, and `.cjs`, so the rules
follow the config wherever it lands.

## Coverage is part of the ordinary quality gate

A project that ships a unit-test runner enforces coverage in the command a
contributor and CI already run. Coverage that only runs behind an extra opt-in
flag reports history, it does not gate anything.

- Install the coverage provider package that matches the installed Vitest
  version (`@vitest/coverage-v8` for `provider: 'v8'`). Its `vitest` peer is an
  exact version, so keep both dependencies on the same range.
- Keep a finite `test:unit` (`vitest run`) for fast local iteration.
- Add `test:coverage` (`vitest run --coverage`).
- Point the ordinary `test` script at `test:coverage`, not at `test:unit`.
- Keep `test:watch` free of coverage; watch mode is for feedback, not gating.

## Per-file thresholds, not repo-wide averages

`coverage.thresholds.perFile` MUST be `true`. A repo-wide average lets
one well-tested file mask a totally uncovered one — coverage gates the
average, not the gap. Per-file enforcement surfaces the gap where it
actually is.

```ts
// ❌ WRONG — repo-wide average; one uncovered file can hide behind dozens
coverage: {
  thresholds: {
    lines: 85,
    branches: 80,
    functions: 85,
    statements: 85,
  },
}

// ✅ CORRECT — every individual file must clear the floor
coverage: {
  thresholds: {
    lines: 85,
    branches: 80,
    functions: 85,
    statements: 85,
    perFile: true,
  },
}
```

## Required minimum floors

- `lines: 85`
- `statements: 85`
- `functions: 85`
- `branches: 80`

Projects may raise these floors as the codebase matures, but must not lower them.
Lowering a floor to fit reality trades a gate for a placebo.

## Explicit production includes

State `coverage.include` explicitly. Without it, coverage only describes the
files a test happened to import, so a production module with no test at all
never appears and never fails. An explicit include list makes an untested
production file show up at zero and break the gate.

```ts
coverage: {
  provider: 'v8',
  include: ['src/**/*.{ts,tsx}'],
}
```

## Legitimate exclusions

Files that can't meaningfully be unit-tested (entry point that just
calls `createRoot`, framework-invoked route shells, vendored generator
output, browser-only mock modules, type-declaration files) belong in
`coverage.exclude`, not in a lowered threshold. The threshold stays honest;
the exclusion list documents why something was carved out.

Do not exclude a module because writing its test is inconvenient, and do not
add import-only or `expect(true)` tests to lift a number. Execution coverage
answers whether tests run production code; it does not claim the assertions
are strong.
