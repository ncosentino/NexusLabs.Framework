; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 0.2.7

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
NLT0001 | Usage    | Warning  | Assert TriedEx and TriedNullEx results directly with NexusLabs.TUnit.Assertions Succeeded() or Failed() instead of passing their Success, Value, or Error properties to TUnit Assert.That.
