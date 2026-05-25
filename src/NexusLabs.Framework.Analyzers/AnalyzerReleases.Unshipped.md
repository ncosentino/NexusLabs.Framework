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
