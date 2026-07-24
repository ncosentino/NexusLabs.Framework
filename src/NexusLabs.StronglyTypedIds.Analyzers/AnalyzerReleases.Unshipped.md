; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
NLS0001 | Usage    | Error    | UUIDv7-enabled identifiers must use their generated Create() method instead of the built-in New() method, which creates UUIDv4 values.
NLS0002 | Usage    | Error    | Do not construct a UUIDv7-enabled identifier directly from Guid.NewGuid(); use the generated Create() method or pass an externally sourced GUID only when intentionally rehydrating an existing identifier.
