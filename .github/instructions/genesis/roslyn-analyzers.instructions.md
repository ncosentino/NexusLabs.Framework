---
applyTo: "**/*Analyzer.cs,**/*CodeFixProvider.cs,**/DiagnosticDescriptors.cs,**/AnalyzerReleases.*.md"
---

# Roslyn Analyzer Rules

## Diagnostic ID convention

Two patterns are valid. Pick one per repo and stay consistent — never mix
within a single package.

### Pattern A: project-prefix + component-codes

Best when a single application has one analyzer project (or a few analyzer
projects under a single product brand). Use a short project-level prefix and
component codes within it:

| Component | Prefix | Example |
|-----------|--------|---------|
| Core | `MYAPPCOR` | `MYAPPCOR001` |
| Generators | `MYAPPGEN` | `MYAPPGEN001` |

The ID stays meaningful even as the analyzer project grows multiple
concern areas.

### Pattern B: per-package short prefix + numeric series

Best for multi-package monorepos where each analyzer package is independently
named, versioned, and installed. Use a short 3-letter prefix per package and a
single 4-digit numeric series:

| Package | Prefix | Example |
|---------|--------|---------|
| `MyCompany.Core.Analyzers` | `MCC` | `MCC0001` |
| `MyCompany.Testing.Analyzers` | `MCT` | `MCT0001` |
| `MyCompany.Data.Analyzers` | `MCD` | `MCD0001` |

Shorter to type; consumers grepping their .editorconfig can immediately
identify which package a suppression targets.

### Universal rules

- Maintain sequential numbering within each prefix.
- Never reuse a retired ID.
- Pick a prefix that does not collide with the well-known ones (`CA*`,
  `SA*`, `IDE*`, `RCS*`, `S*`, `xUnit*`).

## DiagnosticDescriptor category

Use the category that most accurately reflects what the rule is about. Choose
from the conventional Roslyn categories:

| Category | When to use |
|----------|-------------|
| `Usage` | The rule is about HOW an API is used (e.g. "don't call `Console.WriteLine` in library code", "always pass a `CancellationToken`"). Most analyzer rules belong here. |
| `Design` | The rule is about API surface design (e.g. "interfaces in this namespace must start with I", "public types must be sealed"). |
| `Reliability` | The rule prevents likely runtime failures (e.g. "dispose `IDisposable`", "don't ignore `Task` results"). |
| `Performance` | The rule flags inefficient patterns (e.g. "use `StringBuilder` for repeated concatenation", "avoid `string.IsNullOrEmpty` in hot loops"). |
| `Security` | The rule prevents security vulnerabilities (e.g. injection, weak cryptography). |
| `Naming` | The rule enforces naming conventions. |
| `Maintainability` | The rule improves long-term maintainability (e.g. "method is too long"). |

Default to `Usage` if unsure. Do NOT invent custom categories — consumers
filter by category in `.editorconfig` and expect the conventional set.

## Default severity

| Severity | When to use |
|----------|-------------|
| `Error` | The rule represents an unambiguous defect. The diagnostic must be fixed before the code compiles. |
| `Warning` | The rule reflects a strong convention with rare legitimate exceptions. Default for **opt-in analyzer packages** — consumers chose to install you, so warning-level signals are expected. |
| `Info` | The rule is a suggestion. Useful for broadly-installed analyzers (e.g. ones shipping in a base SDK) where most consumers haven't opted in. |
| `Hidden` | The rule only surfaces in IDE refactorings. Almost never the right default. |

For a typical opt-in analyzer package, `Warning` is the right default. The
consumer can suppress per-rule via `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.MYAPPCOR001.severity = none
```

## Release tracking (RS2000)

Every new diagnostic MUST be added to `AnalyzerReleases.Unshipped.md` in the
same project. Format:

```
MYAPPXXX | MyApp.Analyzers | Error | AnalyzerClassName, Short title
```

Forgetting this causes `RS2000` build errors. The `Microsoft.CodeAnalysis.Analyzers`
package enforces this when `EnforceExtendedAnalyzerRules=true` is set.

## Message format (RS1032)

- **Single sentence**: no trailing period. Example: `"Type '{0}' must implement IDisposable"`
- **Multi-sentence**: trailing period on the last sentence. Example: `"Type '{0}' has conflicting attributes. Remove one of them."`

## Writing LLM-actionable diagnostics

Diagnostics are read by humans **and** by LLMs consuming raw `dotnet build` /
`msbuild` output. Build output contains only `title`, `messageFormat`, and
the bare diagnostic ID — the rich `description` field is IDE-hover-only.
Optimise the visible fields for both audiences.

### Each diagnostic MUST be self-sufficient

A future LLM, given only the build line
`MyFile.cs(42,5): error MCC0001: <message>`
should be able to produce a correct fix without re-reading the analyzer
source. That means:

- **Be prescriptive, not just diagnostic.** State what is wrong AND what to
  do. "X is Y" is not enough; say "X is Y; do Z."
- **Name the concrete API or syntax** the caller should switch to. Don't
  say "use the safer overload" — name the overload. Don't say "use the
  proper pattern" — name the type, method, or keyword.
- **Disambiguate when multiple fixes exist.** If there are two valid
  remediations, mention both with the trade-off ("…either A (preferred when
  X) or B (when Y)").
- **Avoid jargon the LLM can't ground.** Project-specific terms are fine
  only if the messageFormat also gives the concrete code shape.

### Per-rule `helpLinkUri` is REQUIRED

Every descriptor MUST set `helpLinkUri` to a stable, rule-specific URL. A
shared link to the repo CHANGELOG.md is not acceptable — IDE quick-info
and LLM tooling both follow this link as the canonical explanation.

Recommended shape:

```
https://github.com/<org>/<repo>/blob/main/docs/analyzers/<RULEID>.md
```

Each rule file contains: one-sentence summary, motivation, **good and bad
code examples**, suppression guidance, related rules.

### Use `description` for breadth, `messageFormat` for the single most
important sentence

- `messageFormat` — the 1–2 sentence prescriptive instruction visible in
  every build error.
- `description` — the longer prose visible on IDE hover. Use it for
  rationale, examples, links to docs. Do NOT put critical remediation
  steps here that aren't also in `messageFormat`, because build-output
  consumers (CI logs, LLMs reading `dotnet build`) never see it.

### BAD vs GOOD examples

**BAD** — diagnostic only, no remediation, LLM has to guess:

```csharp
messageFormat: "Value access on TriedEx without Success check"
helpLinkUri:   "https://github.com/acme/repo/blob/main/CHANGELOG.md"
```

**GOOD** — prescriptive, names the API to use, per-rule link:

```csharp
messageFormat:
    "Accessing TriedEx<T>.Value when Success is false throws. " +
    "Check `if (result.Success)` first, or use `result.Match(...)` to handle both branches."
helpLinkUri:
    "https://github.com/acme/repo/blob/main/docs/analyzers/MCC0002.md"
```

**BAD** — vague:

```csharp
messageFormat: "Improper exception handling pattern"
```

**GOOD** — names the wrapper, names the parameter:

```csharp
messageFormat:
    "Method '{0}' throws inside a Try callback; throw outside the callback " +
    "or return `new Exception(...)` from the lambda so Try captures it as TriedEx.Error"
```

### Formatting checklist for every descriptor

- [ ] Does the messageFormat say WHAT IS WRONG?
- [ ] Does the messageFormat say HOW TO FIX IT (concrete API/syntax)?
- [ ] Is helpLinkUri rule-specific (not a generic CHANGELOG)?
- [ ] Is the title also prescriptive (it appears in IDE error list)?
- [ ] If two remediations exist, are both named with their trade-offs?

## Compilation-end diagnostics (RS1037)

If a diagnostic is reported from a `RegisterCompilationEndAction`, the
descriptor MUST include `customTags: WellKnownDiagnosticTags.CompilationEnd`.

## Analyzer class shape

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MyAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ...;

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(...) or RegisterSymbolAction(...);
    }
}
```

- Always `public sealed class`.
- Always call `ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)` — analyzers should not run on generated code.
- Always call `EnableConcurrentExecution()` for performance.
- Always pass `context.CancellationToken` to semantic-model lookups (`GetSymbolInfo`, `GetTypeInfo`, etc.) so the analyzer honors compiler cancellation.

## Code-fix provider shape

If the analyzer offers an automated fix, ship it as a sibling
`CodeFixProvider`:

```csharp
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MyCodeFixProvider))]
[Shared]
public sealed class MyCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.MyRule.Id);

    public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Apply MyRule fix",
                createChangedDocument: ct => ApplyFixAsync(context.Document, diagnostic, ct),
                equivalenceKey: nameof(MyCodeFixProvider)),
            diagnostic);
    }

    private static async Task<Document> ApplyFixAsync(
        Document document,
        Diagnostic diagnostic,
        CancellationToken cancellationToken) { ... }
}
```

- Always `public sealed class`.
- Always set an `equivalenceKey` so fix-all dedupes correctly across files.
- Return `WellKnownFixAllProviders.BatchFixer` from `GetFixAllProvider` unless the fix is order-sensitive.
- Honor `cancellationToken` on every async hop.

## Suppressing analyzer warnings

`[System.Diagnostics.CodeAnalysis.SuppressMessage]` is **STRICTLY FORBIDDEN**
without explicit team lead approval. Never suppress analyzer warnings to make
code compile. Fix the code instead.

## Packaging an analyzer project

The analyzer project's `.csproj` packaging shape is non-obvious — the canonical
`<IncludeBuildOutput>false</IncludeBuildOutput>` + `<None Pack="true" PackagePath="analyzers/dotnet/cs">`
pattern triggers `NU5017` (a hard pack error not a warning, so `NoWarn` does
not suppress it). The working pattern is:

```xml
<PropertyGroup>
  <TargetFramework>netstandard2.0</TargetFramework>
  <IsRoslynComponent>true</IsRoslynComponent>
  <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
  <DevelopmentDependency>true</DevelopmentDependency>
  <BuildOutputTargetFolder>analyzers/dotnet/cs</BuildOutputTargetFolder>
  <SuppressDependenciesWhenPacking>true</SuppressDependenciesWhenPacking>
  <NoPackageAnalysis>true</NoPackageAnalysis>
</PropertyGroup>
```

The genesis `roslyn-tooling` template ships this configuration in
`Directory.Build.props` plus a `scripts/Verify-Pack.ps1` smoke-test that
installs the packed `.nupkg` into a transient consumer project and verifies
the analyzer dll loads. Run it after `dotnet pack` in CI.

## Disposal-hygiene coverage: IDisposableAnalyzers + Roslynator

Genesis templates ship **Roslynator** as the default code-quality analyzer,
but Roslynator does NOT cover IDisposable lifecycle rules; it defers to the
built-in `CA2000`, which has known false positives and weak
ownership-transfer tracking.

For disposable-correctness coverage, templates also ship
[IDisposableAnalyzers](https://github.com/DotNetAnalyzers/IDisposableAnalyzers)
(the de facto standard, IDISP001–IDISP025). Rules of thumb:

- **Roslynator** handles general code-quality (RCS*).
- **IDisposableAnalyzers** handles disposal-correctness (IDISP*).
- They are complementary; keep both in the analyzer set.
- Do not introduce a third analyzer for the same concern without a
  documented gap.

