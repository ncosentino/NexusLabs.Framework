# NexusLabs.Framework.Analyzers — Rule Catalog

Per-rule reference documentation for the diagnostics shipped in
`NexusLabs.Framework.Analyzers`. Each rule page contains a one-sentence
summary, motivation, **good and bad code examples**, suppression guidance,
and related rules.

These pages are linked from each diagnostic's `helpLinkUri` and are the
canonical explanation surfaced in IDE quick-info and tooling that follows
build-output URLs (including LLM consumers).

`NLF` rules ship in `NexusLabs.Framework.Analyzers`. `NLT` rules ship inside
`NexusLabs.TUnit.Assertions`. `NLS` rules ship inside
`NexusLabs.StronglyTypedIds`.

## Rules

| ID | Title | Severity | Category |
|----|-------|----------|----------|
| [NLF0001](NLF0001.md) | Replace Console/Debug.Write with ILogger in library code | Warning | Usage |
| [NLF0002](NLF0002.md) | Check TriedEx.Success before accessing Value | Warning | Usage |
| [NLF0003](NLF0003.md) | Check TriedEx.Success is false before accessing Error | Warning | Usage |
| [NLF0004](NLF0004.md) | Remove redundant Error null check on Success-false branch | Warning | Usage |
| [NLF0005](NLF0005.md) | Preserve original Error when returning an exception from a Success-false branch | Warning | Usage |
| [NLF0006](NLF0006.md) | Replace whole-body try-catch with Try.Async / Try.GetAsync | Warning | Usage |
| [NLF0007](NLF0007.md) | Pass ILogger to method-scoped Try.Async variant | Warning | Usage |
| [NLF0008](NLF0008.md) | Return exception from Try.Async callback instead of throwing | Warning | Usage |
| [NLF0009](NLF0009.md) | Wrap async method returning TriedEx&lt;T&gt; with Try.GetAsync | Warning | Usage |
| [NLF0010](NLF0010.md) | Place opening triple-quote on its own line, aligned with closing | Warning | Usage |
| [NLF0011](NLF0011.md) | Dispose TriedEx/TriedNullEx/Tried that wraps a disposable value | Warning | Usage |
| [NLF0012](NLF0012.md) | Parameterless [TransfersOwnership] on non-disposable member is inert | Warning | Usage |
| [NLF0013](NLF0013.md) | Use the strongly-typed ID's Parse/TryParse instead of constructing from a pre-parsed backing-type value | Warning | Usage |
| [NLF0014](NLF0014.md) | Specify IFormatProvider on Parse/TryParse when a culture-aware overload exists | Warning | Usage |
| [NLF0015](NLF0015.md) | Try-prefixed methods must return TriedEx&lt;T&gt;, TriedNullEx&lt;T&gt;, or Exception? | Warning | Usage |
| [NLF0016](NLF0016.md) | HashSet&lt;string&gt; must use StringComparer.OrdinalIgnoreCase | Warning | Usage |
| [NLF0017](NLF0017.md) | Carter module must be declared 'public sealed class' | Warning | Usage |
| [NLF0018](NLF0018.md) | CancellationToken parameters must not have a default value | Warning | Usage |
| [NLF0019](NLF0019.md) | Return a shared empty collection instead of allocating one for a read-only result | Warning | Performance |
| [NLF0020](NLF0020.md) | Async methods must declare a CancellationToken parameter | Warning | Usage |
| [NLF0021](NLF0021.md) | Create Moq mocks from a MockRepository, not 'new Mock&lt;T&gt;()' or 'Mock.Of&lt;T&gt;()' | Warning | Usage |
| [NLF0022](NLF0022.md) | Moq mocks must use MockBehavior.Strict | Warning | Usage |
| [NLF0023](NLF0023.md) | Match value types and records with an exact value or It.Is&lt;T&gt;, not It.IsAny&lt;T&gt; | Warning | Usage |
| [NLF0024](NLF0024.md) | Do not copy a rented span handle | Warning | Usage |
| [NLF0025](NLF0025.md) | Forward the available TimeProvider | Warning | Usage |
| [NLF0026](NLF0026.md) | Consider accepting a TimeProvider | Info | Usage |
| [NLF0027](NLF0027.md) | Use System.TimeProvider instead of a custom time abstraction | Warning | Usage |
| [NLF0028](NLF0028.md) | Do not override CreateTimer on a fake time provider | Warning | Usage |
| [NLT0001](NLT0001.md) | Assert Tried results directly with TUnit | Warning | Usage |
| [NLS0001](NLS0001.md) | Use UUIDv7 Create() instead of the generated New() method | Error | Usage |
| [NLS0002](NLS0002.md) | Use UUIDv7 Create() instead of constructing from Guid.NewGuid() | Error | Usage |
