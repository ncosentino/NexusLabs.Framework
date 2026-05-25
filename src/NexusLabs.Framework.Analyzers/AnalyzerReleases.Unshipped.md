; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
NLF0001 | Usage    | Warning  | Do not use Console.Write / Console.WriteLine / Debug.Write / Debug.WriteLine in library code. Route output through ILogger or similar.
NLF0002 | Usage    | Warning  | Do not access TriedEx/TriedNullEx Value without first checking Success is true.
NLF0003 | Usage    | Warning  | Do not access TriedEx/TriedNullEx Error without first checking Success is false.
NLF0004 | Usage    | Warning  | Redundant null check on TriedEx/TriedNullEx Error after Success has been established as false (Error is guaranteed non-null).
NLF0005 | Usage    | Warning  | When returning an exception from a TriedEx/TriedNullEx Success-false branch, preserve the original Error (return it, wrap as inner, or include in aggregate).
NLF0006 | Usage    | Warning  | Async method whose entire body is a single try-catch should use Try.Async / Try.GetAsync / Try.GetOrNullAsync.
NLF0007 | Usage    | Warning  | Method-scoped Try.Async variants should be invoked with an ILogger argument.
NLF0008 | Usage    | Warning  | Do not throw inside a Try.Async variant callback — return the exception instead.
NLF0009 | Usage    | Warning  | Async method returning Task&lt;TriedEx&lt;T&gt;&gt;/Task&lt;TriedNullEx&lt;T&gt;&gt; should wrap its body with Try.GetAsync / Try.GetOrNullAsync (direct pass-through is allowed).
NLF0010 | Usage    | Warning  | Multi-line raw string literal opening triple-quote must be on its own line, aligned with the closing triple-quote.
