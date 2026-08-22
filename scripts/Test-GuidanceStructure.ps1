#Requires -Version 7.0
<#
.SYNOPSIS
    Validates the repository guidance contract declared in
    .github/genesis-guidance.json.

.DESCRIPTION
    Enforces the structural rules that keep agent guidance loadable and
    correctly owned: root entrypoint budgets, docs-map reachability, contract
    conformance, matched instruction context ceilings, and the ownership
    guarantees this repository relies on.
#>
[CmdletBinding()]
param(
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'

if (-not $ProjectRoot) {
    $ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
}
$ProjectRoot = (Resolve-Path $ProjectRoot).Path
Push-Location $ProjectRoot
try {
    $failures = [System.Collections.Generic.List[string]]::new()
    function Add-Failure([string]$message) { $failures.Add($message) }
    function Test-Rule([string]$name, [bool]$ok, [string]$detail) {
        if ($ok) {
            Write-Host "  PASS  $name"
        }
        else {
            Write-Host "  FAIL  $name - $detail"
            Add-Failure "$name - $detail"
        }
    }

    $contractPath = '.github/genesis-guidance.json'
    if (-not (Test-Path $contractPath)) {
        throw "Guidance contract not found at '$contractPath'."
    }
    $contractRaw = Get-Content $contractPath -Raw
    $contract = $contractRaw | ConvertFrom-Json

    Write-Host 'Contract'
    $schemaPath = '.github/genesis-guidance.schema.json'
    Test-Rule 'contract matches schema' `
        ([bool](Test-Json -Json $contractRaw -SchemaFile $schemaPath -ErrorAction SilentlyContinue)) `
        "'$contractPath' does not validate against '$schemaPath'"

    Write-Host 'Root entrypoints'
    $agentsPath = $contract.agents.path
    $agentLines = @(Get-Content $agentsPath).Count
    $agentBytes = (Get-Item $agentsPath).Length
    Test-Rule "$agentsPath line budget" `
        ($agentLines -le $contract.agents.maxLines) `
        "$agentLines lines exceeds $($contract.agents.maxLines)"
    Test-Rule "$agentsPath byte budget" `
        ($agentBytes -le $contract.agents.maxBytes) `
        "$agentBytes bytes exceeds $($contract.agents.maxBytes)"

    $claudePath = $contract.agents.redirects.claude
    Test-Rule "$claudePath redirects to $agentsPath" `
        ((Test-Path $claudePath) -and ((Get-Content $claudePath -Raw) -match [regex]::Escape($agentsPath))) `
        "expected a redirect to $agentsPath"

    $copilotPath = $contract.agents.redirects.copilot
    Test-Rule "$copilotPath points at $agentsPath" `
        ((Test-Path $copilotPath) -and ((Get-Content $copilotPath -Raw) -match [regex]::Escape($agentsPath))) `
        "expected a pointer to $agentsPath"

    Write-Host 'Documentation map'
    $mapPath = $contract.docs.mapPath
    Test-Rule 'docs map exists' (Test-Path $mapPath) "missing '$mapPath'"
    if (Test-Path $mapPath) {
        $mapDir = Split-Path $mapPath -Parent
        $mapText = Get-Content $mapPath -Raw
        foreach ($match in [regex]::Matches($mapText, '\]\(([^)#][^)]*)\)')) {
            $link = $match.Groups[1].Value
            if ($link -match '^[a-z][a-z0-9+.-]*:') { continue }
            $resolved = Join-Path $mapDir $link
            Test-Rule "map link '$link' resolves" (Test-Path $resolved) 'target not found'
        }
        foreach ($page in $contract.docs.pages) {
            Test-Rule "declared page '$($page.path)' exists" (Test-Path $page.path) 'declared in the contract but missing'
            $relative = [IO.Path]::GetRelativePath($mapDir, $page.path).Replace('\', '/')
            Test-Rule "declared page '$($page.path)' is reachable from the map" `
                ($mapText -match [regex]::Escape($relative)) `
                'not linked from the documentation map'
        }
    }

    Write-Host 'Instruction context'
    $resolver = $contract.review.instructionResolver
    Test-Rule 'instruction resolver exists' (Test-Path $resolver) "missing '$resolver'"
    Test-Rule 'validation inventory exists' (Test-Path $contract.review.validationInventory) 'missing'
    Test-Rule 'review skill exists' (Test-Path $contract.review.skillPath) 'missing'

    # Worst-case populations rather than a convenience sample: the broadest C#
    # test and source globs pull the largest matched stacks in this repository.
    $representative = @(
        'tests/NexusLabs.Framework.Analyzers.Tests/AsyncMethodCancellationTokenAnalyzerTests.cs',
        'src/NexusLabs.Framework.Analyzers/AsyncMethodCancellationTokenAnalyzer.cs',
        'src/NexusLabs.Framework/Buffers/RentedMemory.cs'
    ) | Where-Object { Test-Path $_ }

    $limits = $contract.instructions.matchedContext
    foreach ($path in $representative) {
        $lines = 0
        $bytes = 0
        foreach ($entry in & $resolver -Path $path) {
            $instruction = $entry.InstructionPath
            if (Test-Path $instruction) {
                $lines += @(Get-Content $instruction).Count
                $bytes += (Get-Item $instruction).Length
            }
        }
        Test-Rule "matched context within ceiling: $path" `
            ($lines -le $limits.maxLines -and $bytes -le $limits.maxBytes) `
            "$lines lines / $bytes bytes exceeds $($limits.maxLines) lines / $($limits.maxBytes) bytes"
    }

    Write-Host 'Ownership guarantees'
    $managedRoot = $contract.instructions.managedRoot
    # The root file keeps only a pointer to the result pattern, so an upstream
    # refresh that dropped the rule would leave it unowned and unenforced.
    $resultPatternOwned = @(
        Get-ChildItem $managedRoot -Recurse -Filter *.md |
            Select-String -Pattern 'TriedEx' -SimpleMatch -List
    ).Count
    Test-Rule 'result pattern is owned by an instruction' `
        ($resultPatternOwned -gt 0) `
        "no instruction under '$managedRoot' mentions TriedEx"

    # Capabilities declined in the profile removed their instructions. If those
    # file populations ever appear, the guidance must be restored first.
    $profilePath = '.github/instruction-profile.json'
    if (Test-Path $profilePath) {
        $declined = @((Get-Content $profilePath -Raw | ConvertFrom-Json).selection.declined.id)
        $uiCapabilities = @('avalonia', 'maui', 'wpf')
        if (@($declined | Where-Object { $_ -in $uiCapabilities }).Count -gt 0) {
            $uiFiles = @(
                git ls-files |
                    Where-Object { $_ -match '\.(axaml|xaml|resx)$' -or $_ -match 'ViewModel\.cs$' }
            )
            Test-Rule 'no UI sources while UI capabilities are declined' `
                ($uiFiles.Count -eq 0) `
                "found $($uiFiles.Count) UI file(s); re-run the instruction profile sync to restore UI guidance"
        }
    }

    Write-Host ''
    if ($failures.Count -gt 0) {
        Write-Host "Guidance structure validation FAILED with $($failures.Count) issue(s)."
        exit 1
    }
    Write-Host 'Guidance structure validation passed.'
}
finally {
    Pop-Location
}
