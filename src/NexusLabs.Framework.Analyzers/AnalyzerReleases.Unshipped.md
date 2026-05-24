; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
NLF0001 | Usage    | Warning  | Do not use Console.Write / Console.WriteLine / Debug.Write / Debug.WriteLine in library code. Route output through ILogger or similar.
