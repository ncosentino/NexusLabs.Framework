#Requires -Version 7.0
<#
.SYNOPSIS
    Plans or configures PR-first GitHub delivery for a generated project.

.DESCRIPTION
    Reads `.github/genesis-delivery.json`, probes repository settings and branch
    protection capability, active default-branch rules, and existing protection,
    then selects native protected auto-merge or the private exact-SHA
    workflow-run lane.

    The default is plan-only. `-Apply` is required for GitHub writes. This
    script never commits, pushes, opens a pull request, or merges. Active
    rulesets and incompatible existing protection require manual reconciliation.

.PARAMETER ProjectRoot
    Generated project root. Defaults to the current directory.

.PARAMETER Repository
    Optional `owner/repository`. Defaults to the current gh repository.

.PARAMETER DeliveryMode
    Auto, Native, or WorkflowRun. Auto selects Native whenever protection is
    available and WorkflowRun only for the known private-plan denial.

.PARAMETER CheckSha
    Commit whose check runs establish the required GitHub check contexts.
    Defaults to the remote default branch tip.

.PARAMETER Apply
    Apply the planned GitHub settings. Without this switch, no writes occur.

.PARAMETER AllowUnprotectedPrivate
    Required with `-Apply` when WorkflowRun is selected.

.PARAMETER ReviewPolicy
    None, or CopilotOneApproval. CopilotOneApproval requires one trusted human
    approval on the current SHA before a ready Copilot-authored PR can merge.

.PARAMETER ControlRunner
    Runner label or hosted image used by deterministic repository controls. Public
    repositories default to ubuntu-24.04. Private repositories require an explicit
    value; ubuntu-24.04 is the manual hosted-recovery choice.

.PARAMETER SnapshotPath
    Optional offline repository snapshot used by Genesis contract tests.

.PARAMETER AuditProtectionDrift
    Return the deterministic delivery-protection drift assessment without planning
    or applying repository mutations.

.PARAMETER GhCommand
    gh executable path. Defaults to `gh`.

.EXAMPLE
    ./scripts/delivery/Configure-GitHubDelivery.ps1

.EXAMPLE
    ./scripts/delivery/Configure-GitHubDelivery.ps1 -Apply
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$ProjectRoot = (Get-Location).Path,
    [string]$Repository,
    [ValidateSet('Auto', 'Native', 'WorkflowRun')]
    [string]$DeliveryMode = 'Auto',
    [string]$CheckSha,
    [switch]$Apply,
    [switch]$AllowUnprotectedPrivate,
    [ValidateSet('None', 'CopilotOneApproval')]
    [string]$ReviewPolicy = 'None',
    [string]$ControlRunner,
    [string]$SnapshotPath,
    [switch]$AuditProtectionDrift,
    [string]$GhCommand = 'gh'
)

$ErrorActionPreference = 'Stop'

$ProjectRoot = (Resolve-Path $ProjectRoot).Path
$contractPath = Join-Path $ProjectRoot '.github' 'genesis-delivery.json'
$installerPath = Join-Path $ProjectRoot 'scripts' 'delivery' 'Install-PrivateAutoMerge.ps1'

if (-not (Test-Path $contractPath)) {
    Write-Error "Delivery contract not found at '$contractPath'."
}

$contract = Get-Content $contractPath -Raw | ConvertFrom-Json
$stackedPullRequests = @(
    $contract.componentWorkflows |
        Where-Object {
            [string]$_.source -ceq 'github-stacked-pr-delivery' -and
            [string]$_.path -ceq '.github/workflows/pr-base.yml'
        }
).Count -eq 1
$requiredChecks = @($contract.requiredChecks | ForEach-Object { [string]$_ })
$draftMode =
    if (@($contract.runnerProfiles).Count -gt 0) {
        [string]$contract.draftValidation.pitcrewDefault
    } else {
        [string]$contract.draftValidation.hostedDefault
    }

function Test-ObjectProperties {
    param(
        [AllowNull()]
        [object]$Value,

        [Parameter(Mandatory)]
        [string[]]$Names
    )

    if ($null -eq $Value) {
        return $false
    }
    $propertyNames = @($Value.PSObject.Properties.Name)
    return @($Names | Where-Object { $_ -notin $propertyNames }).Count -eq 0
}

function Test-EmptyActorCollections {
    param([AllowNull()][object]$Value)

    if ($null -eq $Value) {
        return $true
    }
    return (
        @($Value.users | Where-Object { $null -ne $_ }).Count -eq 0 -and
        @($Value.teams | Where-Object { $null -ne $_ }).Count -eq 0 -and
        @($Value.apps | Where-Object { $null -ne $_ }).Count -eq 0
    )
}

function Test-LegacyZeroReviewProtection {
    param([AllowNull()][object]$ReviewRequirement)

    if (-not (Test-ObjectProperties `
        -Value $ReviewRequirement `
        -Names @(
            'dismiss_stale_reviews',
            'require_code_owner_reviews',
            'required_approving_review_count',
            'require_last_push_approval'
        ))) {
        return $false
    }
    return (
        [int]$ReviewRequirement.required_approving_review_count -eq 0 -and
        -not [bool]$ReviewRequirement.dismiss_stale_reviews -and
        -not [bool]$ReviewRequirement.require_code_owner_reviews -and
        -not [bool]$ReviewRequirement.require_last_push_approval -and
        (Test-EmptyActorCollections `
            -Value $ReviewRequirement.bypass_pull_request_allowances) -and
        (Test-EmptyActorCollections `
            -Value $ReviewRequirement.dismissal_restrictions)
    )
}

function Test-CheckCollectionsEqual {
    param(
        [Parameter(Mandatory)]
        [object[]]$Left,

        [Parameter(Mandatory)]
        [object[]]$Right
    )

    $leftKeys = @(
        $Left |
            ForEach-Object {
                "$([string]$_.context):$([int]$_.app_id)"
            } |
            Sort-Object -Unique
    )
    $rightKeys = @(
        $Right |
            ForEach-Object {
                "$([string]$_.context):$([int]$_.app_id)"
            } |
            Sort-Object -Unique
    )
    return ($leftKeys -join ',') -ceq ($rightKeys -join ',')
}

function Test-ClassicProtectionSafety {
    param(
        [AllowNull()]
        [object]$Protection,

        [Parameter(Mandatory)]
        [string[]]$RequiredChecks
    )

    if (-not (Test-ObjectProperties `
        -Value $Protection `
        -Names @(
            'required_status_checks',
            'enforce_admins',
            'required_conversation_resolution',
            'allow_force_pushes',
            'allow_deletions',
            'required_linear_history',
            'block_creations',
            'lock_branch',
            'allow_fork_syncing',
            'required_signatures'
        ))) {
        return $false
    }
    $existingChecks = @($Protection.required_status_checks.checks)
    $missingChecks = @(
        $RequiredChecks |
            Where-Object {
                $requiredCheck = $_
                -not ($existingChecks | Where-Object {
                    [string]$_.context -ceq $requiredCheck -and
                    [int]$_.app_id -eq 15368
                })
            }
    )
    return (
        $missingChecks.Count -eq 0 -and
        [bool]$Protection.required_status_checks.strict -and
        [bool]$Protection.enforce_admins.enabled -and
        -not [bool]$Protection.allow_force_pushes.enabled -and
        -not [bool]$Protection.allow_deletions.enabled -and
        -not [bool]$Protection.required_linear_history.enabled -and
        -not [bool]$Protection.block_creations.enabled -and
        -not [bool]$Protection.lock_branch.enabled -and
        -not [bool]$Protection.allow_fork_syncing.enabled -and
        -not [bool]$Protection.required_signatures.enabled -and
        $null -eq $Protection.restrictions
    )
}

function Test-DesiredClassicProtection {
    param(
        [AllowNull()]
        [object]$Protection,

        [Parameter(Mandatory)]
        [object[]]$ExpectedChecks
    )

    return (
        (Test-ClassicProtectionSafety `
            -Protection $Protection `
            -RequiredChecks @($ExpectedChecks | ForEach-Object { [string]$_.context })) -and
        (Test-CheckCollectionsEqual `
            -Left @($Protection.required_status_checks.checks) `
            -Right $ExpectedChecks) -and
        $null -eq $Protection.required_pull_request_reviews -and
        -not [bool]$Protection.required_conversation_resolution.enabled -and
        -not [bool]$Protection.required_linear_history.enabled -and
        -not [bool]$Protection.block_creations.enabled -and
        -not [bool]$Protection.lock_branch.enabled -and
        -not [bool]$Protection.allow_fork_syncing.enabled
    )
}

function New-ClassicProtectionBody {
    param(
        [Parameter(Mandatory)]
        [object[]]$Checks
    )

    return [ordered]@{
        required_status_checks = [ordered]@{
            strict = $true
            checks = @(
                $Checks |
                    ForEach-Object {
                        [ordered]@{
                            context = [string]$_.context
                            app_id = [int]$_.app_id
                        }
                    }
            )
        }
        enforce_admins = $true
        required_pull_request_reviews = $null
        restrictions = $null
        required_linear_history = $false
        allow_force_pushes = $false
        allow_deletions = $false
        block_creations = $false
        required_conversation_resolution = $false
        lock_branch = $false
        allow_fork_syncing = $false
    }
}

function Invoke-GhJson {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    $output = & $GhCommand @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "gh $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
    if ([string]::IsNullOrWhiteSpace(($output -join "`n"))) {
        return $null
    }
    return (($output -join "`n") | ConvertFrom-Json)
}

function Test-LegacyMergeAutomation {
    param([string]$WorkflowText)

    foreach ($pattern in @(
        '(?i)\bgh\s+pr\s+merge\b',
        '(?i)\b(?:github|octokit)\.rest\.pulls\.merge\b',
        '(?i)\benablePullRequestAutoMerge\b',
        '(?i)\bmergePullRequest\b',
        '(?i)repos/.+/pulls/.+/merge',
        '(?i)\benable-pull-request-automerge\b',
        '(?i)\b(?:automerge|auto-merge)[-/]action\b',
        '(?im)^\s*-\s*uses:\s*[^#\r\n]*(?:auto.?merge|automerge|merge-me|dependabot-auto-merge)[^#\r\n]*@'
    )) {
        if ($WorkflowText -match $pattern) {
            return $true
        }
    }
    $hasPullRequestWrite = (
        $WorkflowText -match '(?i)\bpull-requests\s*:\s*write\b' -or
        $WorkflowText -match '(?i)\bpermissions\s*:\s*write-all\b'
    )
    return (
        $hasPullRequestWrite -and
        $WorkflowText -match '(?im)^\s*-\s*uses:\s+\./')
}

function Test-StandardPrivateAutoMerge {
    param([string]$WorkflowText)

    return (
        $WorkflowText -match "GENESIS_DELIVERY_MODEL == 'workflow-run'" -and
        $WorkflowText -notmatch 'actions/checkout' -and
        $WorkflowText -match 'head\.repo\.full_name == \$repo' -and
        $WorkflowText -match '-f sha="\$tested_sha"' -and
        $WorkflowText -match '\.app\.id == \$app_id' -and
        $WorkflowText -match '--force-with-lease=\$\{full_ref\}:\$\{tested_sha\}'
    )
}

function Get-LegacyMergeWorkflowPaths {
    $workflowsRoot = Join-Path $ProjectRoot '.github' 'workflows'
    if (-not (Test-Path -LiteralPath $workflowsRoot -PathType Container)) {
        return @()
    }

    return @(
        Get-ChildItem -LiteralPath $workflowsRoot -File |
            Where-Object { $_.Extension -in @('.yml', '.yaml') } |
            Where-Object {
                $text = Get-Content -LiteralPath $_.FullName -Raw
                if ($_.Name -ceq 'private-auto-merge.yml') {
                    -not (Test-StandardPrivateAutoMerge -WorkflowText $text)
                } else {
                    Test-LegacyMergeAutomation -WorkflowText $text
                }
            } |
            ForEach-Object {
                $_.FullName.Substring($ProjectRoot.Length + 1) -replace '\\', '/'
            } |
            Sort-Object -Unique)
}

function Get-NativeMissingChecksNextAction {
    param(
        [Parameter(Mandatory)]
        [string[]]$MissingChecks,

        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [string[]]$BootstrapBlockers
    )

    if ($MissingChecks.Count -eq 1 -and $MissingChecks[0] -ceq 'Review policy') {
        if ($BootstrapBlockers.Count -gt 0) {
            return "Review policy became available only after the migration reached the default branch. Empty-commit bootstrap PR mode is unsafe here: $($BootstrapBlockers -join '; '). Request a real harmless change PR that satisfies CI, PR title, Review policy, and every repository-specific required check, rerun activation with that PR's exact head SHA via -CheckSha, and do not weaken workflows or add permanent marker files."
        }

        return 'Review policy became available only after the migration reached the default branch. Use a genuine ready PR when available; otherwise, only with explicit user approval, create a bootstrap branch with `git commit --allow-empty -m "chore: bootstrap Genesis delivery activation"` and open a ready PR titled `chore: bootstrap Genesis delivery activation`. Verify CI, PR title, Review policy, and every repository-specific required check on that PR''s exact head SHA, rerun activation with -CheckSha, require a separate explicit approval before -Apply, then arm squash auto-merge only after activation so the bootstrap PR itself proves merge plus head-branch deletion.'
    }

    return "Run the required workflows, then rerun with -CheckSha <sha>. Missing: $($MissingChecks -join ', ')."
}

function Get-EmptyBootstrapPrBlockers {
    param(
        [Parameter(Mandatory)]
        [string]$ProjectRoot
    )

    $workflowsRoot = Join-Path $ProjectRoot '.github' 'workflows'
    if (-not (Test-Path -LiteralPath $workflowsRoot -PathType Container)) {
        return @()
    }

    $blockers = [System.Collections.Generic.List[string]]::new()
    $prOpenTypes = @('opened', 'synchronize', 'reopened', 'ready_for_review')
    foreach ($workflowPath in @(
        Get-ChildItem -LiteralPath $workflowsRoot -File |
            Where-Object { $_.Extension -in @('.yml', '.yaml') } |
            ForEach-Object { $_.FullName } |
            Sort-Object
    )) {
        $text = Get-Content -LiteralPath $workflowPath -Raw
        $relativePath = $workflowPath.Substring($ProjectRoot.Length + 1) -replace '\\', '/'
        $hasPrTrigger = (
            $text -match '(?im)^\s*pull_request(?:_target)?\s*:' -or
            $text -match '(?im)^\s*on\s*:\s*\[[^\]]*\bpull_request(?:_target)?\b'
        )
        if (-not $hasPrTrigger) {
            continue
        }

        if ($text -match '(?im)^\s*paths(?:-ignore)?\s*:') {
            $blockers.Add("$relativePath uses paths or paths-ignore filters for pull-request events") | Out-Null
            continue
        }

        $typeFilter = [regex]::Match(
            $text,
            '(?ims)^\s*pull_request(?:_target)?\s*:\s*(?<body>(?:\s{2,}.*\r?\n)+)'
        )
        if (-not $typeFilter.Success) {
            continue
        }

        $body = $typeFilter.Groups['body'].Value
        if ($body -notmatch '(?im)^\s*types\s*:') {
            continue
        }

        $allowsPrOpenEvent = $false
        foreach ($event in $prOpenTypes) {
            if ($body -match "(?im)(?:^\s*-\s*$event\s*$|^\s*types\s*:\s*\[[^\]]*\b$event\b)") {
                $allowsPrOpenEvent = $true
                break
            }
        }

        if (-not $allowsPrOpenEvent) {
            $blockers.Add("$relativePath restricts pull_request types away from opened/synchronize/reopened/ready_for_review") | Out-Null
        }
    }

    return @($blockers)
}

function Get-LiveSnapshot {
    & $GhCommand auth status *> $null
    if ($LASTEXITCODE -ne 0) {
        throw 'GitHub CLI authentication is required. Run gh auth login.'
    }

    $resolvedRepository =
        if ([string]::IsNullOrWhiteSpace($Repository)) {
            $repoView = Invoke-GhJson -Arguments @(
                'repo', 'view', '--json', 'nameWithOwner')
            [string]$repoView.nameWithOwner
        } else {
            $Repository
        }
    if ($resolvedRepository -notmatch '^[^/]+/[^/]+$') {
        throw "Repository '$resolvedRepository' must be owner/name."
    }

    $repositoryState = Invoke-GhJson -Arguments @(
        'api', '-H', 'X-GitHub-Api-Version: 2026-03-10',
        "repos/$resolvedRepository")
    $defaultBranch = [string]$repositoryState.default_branch
    $remoteLegacyMergeWorkflows = [System.Collections.Generic.List[string]]::new()
    $remoteWorkflowEntries = Invoke-GhJson -Arguments @(
        'api',
        '-H', 'X-GitHub-Api-Version: 2026-03-10',
        "repos/$resolvedRepository/contents/.github/workflows?ref=$defaultBranch")
    foreach ($entry in @($remoteWorkflowEntries)) {
        $path = [string]$entry.path
        if (
            [string]$entry.type -cne 'file' -or
            [IO.Path]::GetExtension($path) -notin @('.yml', '.yaml')
        ) {
            continue
        }
        $workflowOutput = & $GhCommand api `
            -H 'Accept: application/vnd.github.raw+json' `
            -H 'X-GitHub-Api-Version: 2026-03-10' `
            "repos/$resolvedRepository/contents/${path}?ref=$defaultBranch"
        if ($LASTEXITCODE -ne 0) {
            throw "Unable to inspect remote workflow '$path'."
        }
        $workflowText = $workflowOutput -join "`n"
        $isLegacy =
            if ([IO.Path]::GetFileName($path) -ceq 'private-auto-merge.yml') {
                -not (Test-StandardPrivateAutoMerge -WorkflowText $workflowText)
            } else {
                Test-LegacyMergeAutomation -WorkflowText $workflowText
            }
        if ($isLegacy) {
            $remoteLegacyMergeWorkflows.Add($path) | Out-Null
        }
    }
    $branchSha =
        if ([string]::IsNullOrWhiteSpace($CheckSha)) {
            $commit = Invoke-GhJson -Arguments @(
                'api', '-H', 'X-GitHub-Api-Version: 2026-03-10',
                "repos/$resolvedRepository/commits/$defaultBranch")
            [string]$commit.sha
        } else {
            $CheckSha
        }

    $protectionStatus = 'unknown'
    $protection = $null
    $protectionOutput = & $GhCommand api `
        -H 'X-GitHub-Api-Version: 2026-03-10' `
        "repos/$resolvedRepository/branches/$defaultBranch/protection" 2>&1
    if ($LASTEXITCODE -eq 0) {
        $protectionStatus = 'protected'
        $protection = ($protectionOutput -join "`n") | ConvertFrom-Json
    } else {
        $message = $protectionOutput -join "`n"
        if ($message -match 'HTTP 404') {
            $protectionStatus = 'available-unprotected'
        } elseif (
            [bool]$repositoryState.private -and
            $message -match '(?i)upgrade.*pro|make this repository public') {
            $protectionStatus = 'unavailable-private-plan'
        } else {
            throw "Unable to determine branch protection capability: $message"
        }
    }

    $activeRulesStatus = 'available'
    $activeRules = @()
    $repositoryRulesetsStatus = 'available'
    $repositoryRulesets = @()
    $activeRulesOutput = & $GhCommand api --paginate --slurp `
        -H 'X-GitHub-Api-Version: 2026-03-10' `
        "repos/$resolvedRepository/rules/branches/${defaultBranch}?per_page=100" 2>&1
    if ($LASTEXITCODE -eq 0) {
        $activeRulePages = ($activeRulesOutput -join "`n") | ConvertFrom-Json
        $activeRules = @(
            foreach ($page in @($activeRulePages)) {
                @($page)
            }
        )
    } else {
        $message = $activeRulesOutput -join "`n"
        if (
            [bool]$repositoryState.private -and
            $message -match '(?i)upgrade.*pro|make this repository public'
        ) {
            $activeRulesStatus = 'unavailable-private-plan'
        } else {
            throw "Unable to inspect active default-branch rules: $message"
        }
    }
    if ($activeRulesStatus -ceq 'available') {
        $rulesetsOutput = & $GhCommand api --paginate --slurp `
            -H 'X-GitHub-Api-Version: 2026-03-10' `
            "repos/$resolvedRepository/rulesets?includes_parents=true&targets=branch&per_page=100" 2>&1
        if ($LASTEXITCODE -eq 0) {
            $rulesetPages = ($rulesetsOutput -join "`n") | ConvertFrom-Json
            $repositoryRulesets = @(
                foreach ($page in @($rulesetPages)) {
                    @($page)
                }
            )
        } else {
            $message = $rulesetsOutput -join "`n"
            if (
                [bool]$repositoryState.private -and
                $message -match '(?i)upgrade.*pro|make this repository public'
            ) {
                $repositoryRulesetsStatus = 'unavailable-private-plan'
            } else {
                throw "Unable to inspect repository rulesets: $message"
            }
        }
    } else {
        $repositoryRulesetsStatus = $activeRulesStatus
    }

    $checkRuns = Invoke-GhJson -Arguments @(
        'api', '--paginate', '--slurp',
        "repos/$resolvedRepository/commits/$branchSha/check-runs?filter=latest&per_page=100")
    $flattenedChecks = @(
        foreach ($page in @($checkRuns)) {
            @($page.check_runs)
        }
    )

    $privateWorkflowPresent = $false
    $workflowOutput = & $GhCommand api `
        -H 'X-GitHub-Api-Version: 2026-03-10' `
        "repos/$resolvedRepository/contents/.github/workflows/private-auto-merge.yml?ref=$defaultBranch" 2>&1
    if ($LASTEXITCODE -eq 0) {
        $privateWorkflowPresent = $true
    } elseif (($workflowOutput -join "`n") -notmatch 'HTTP 404') {
        throw "Unable to inspect private auto-merge workflow: $($workflowOutput -join "`n")"
    }

    return [PSCustomObject]@{
        repository             = $resolvedRepository
        visibility             = [string]$repositoryState.visibility
        private                = [bool]$repositoryState.private
        defaultBranch          = $defaultBranch
        checkSha               = $branchSha
        protectionStatus       = $protectionStatus
        protection             = $protection
        activeRulesStatus      = $activeRulesStatus
        activeRules            = @($activeRules)
        repositoryRulesetsStatus = $repositoryRulesetsStatus
        repositoryRulesets     = @($repositoryRulesets)
        remoteLegacyMergeWorkflows = @($remoteLegacyMergeWorkflows)
        checkRuns              = @($flattenedChecks)
        privateWorkflowPresent = $privateWorkflowPresent
    }
}

$snapshot =
    if ([string]::IsNullOrWhiteSpace($SnapshotPath)) {
        Get-LiveSnapshot
    } else {
        Get-Content (Resolve-Path $SnapshotPath) -Raw | ConvertFrom-Json
    }

if (
    $Apply -and
    -not [string]::IsNullOrWhiteSpace($SnapshotPath) -and
    -not $WhatIfPreference
) {
    throw 'Offline snapshots are plan-only and cannot be used with -Apply.'
}
if ($AuditProtectionDrift -and $Apply) {
    throw '-AuditProtectionDrift cannot be combined with -Apply.'
}

if ([string]::IsNullOrWhiteSpace($Repository)) {
    $Repository = [string]$snapshot.repository
}

$repositoryRulesetsStatus =
    if ($snapshot.PSObject.Properties.Name -contains 'repositoryRulesetsStatus') {
        [string]$snapshot.repositoryRulesetsStatus
    } else {
        'unknown'
    }
$selectedMode =
    if ($DeliveryMode -ne 'Auto') {
        $DeliveryMode
    } elseif ([string]$snapshot.protectionStatus -eq 'unavailable-private-plan') {
        'WorkflowRun'
    } else {
        'Native'
    }

if ($selectedMode -eq 'WorkflowRun' -and -not [bool]$snapshot.private) {
    throw 'WorkflowRun delivery is only supported for private repositories.'
}
if (
    $selectedMode -eq 'Native' -and
    [string]$snapshot.protectionStatus -eq 'unavailable-private-plan' -and
    -not $AuditProtectionDrift
) {
    throw 'Native delivery was requested but branch protection is unavailable for this private repository.'
}

$successfulCheckNames = @(
    @($snapshot.checkRuns) |
        Where-Object { [string]$_.conclusion -eq 'success' } |
        ForEach-Object { [string]$_.name } |
        Sort-Object -Unique
)
$missingChecks = @($requiredChecks | Where-Object { $_ -notin $successfulCheckNames })
$bootstrapBlockers = @(Get-EmptyBootstrapPrBlockers -ProjectRoot $ProjectRoot)

$operations = [System.Collections.Generic.List[string]]::new()
$status = 'ready'
$nextAction = $null
$effectiveControlRunner =
    if (-not [string]::IsNullOrWhiteSpace($ControlRunner)) {
        $ControlRunner
    } elseif (-not [bool]$snapshot.private) {
        'ubuntu-24.04'
    } else {
        $null
    }
$activeRules = @($snapshot.activeRules)
$repositoryRulesets = @(
    if ($snapshot.PSObject.Properties.Name -contains 'repositoryRulesets') {
        @($snapshot.repositoryRulesets)
    }
)
$rulesetConflict = (
    $activeRules.Count -gt 0 -or
    $repositoryRulesets.Count -gt 0 -or
    ($selectedMode -eq 'Native' -and $repositoryRulesetsStatus -cne 'available')
)

$driftProtectedChecks = @()
$driftMissingProtectedChecks = @()
$driftUnexpectedProtectedChecks = @()
$driftReasons = [System.Collections.Generic.List[string]]::new()
$driftStatus = 'not-applicable'

if ($selectedMode -ceq 'Native') {
    $expectedNativeChecks = @(
        $requiredChecks | ForEach-Object {
            [ordered]@{ context = [string]$_; app_id = 15368 }
        }
    )
    if ([string]$snapshot.protectionStatus -ceq 'protected') {
        $driftProtectedChecks = @(
            @($snapshot.protection.required_status_checks.checks) |
                Sort-Object `
                    @{ Expression = { [string]$_.context } },
                    @{ Expression = { [int]$_.app_id } }
        )
        $driftMissingProtectedChecks = @(
            $expectedNativeChecks | Where-Object {
                $ctx = [string]$_.context
                $aid = [int]$_.app_id
                -not ($driftProtectedChecks | Where-Object {
                    [string]$_.context -ceq $ctx -and [int]$_.app_id -eq $aid
                })
            }
        )
        $driftUnexpectedProtectedChecks = @(
            $driftProtectedChecks | Where-Object {
                $ctx = [string]$_.context
                $aid = [int]$_.app_id
                -not ($expectedNativeChecks | Where-Object {
                    [string]$_.context -ceq $ctx -and [int]$_.app_id -eq $aid
                })
            }
        )
        if ($driftMissingProtectedChecks.Count -gt 0) {
            $driftReasons.Add(
                "Missing protected checks: $(($driftMissingProtectedChecks | ForEach-Object { [string]$_.context }) -join ', ')"
            ) | Out-Null
        }
        if ($driftUnexpectedProtectedChecks.Count -gt 0) {
            $driftReasons.Add(
                "Unexpected protected checks: $(($driftUnexpectedProtectedChecks | ForEach-Object { "$([string]$_.context):$([int]$_.app_id)" }) -join ', ')"
            ) | Out-Null
        }
        $wrongAppIdChecks = @(
            $driftProtectedChecks | Where-Object {
                $ctx = [string]$_.context
                $aid = [int]$_.app_id
                ($requiredChecks -contains $ctx) -and $aid -ne 15368
            }
        )
        if ($wrongAppIdChecks.Count -gt 0) {
            $driftReasons.Add(
                "Wrong app_id for checks: $(($wrongAppIdChecks | ForEach-Object { "$([string]$_.context):$([int]$_.app_id)" }) -join ', ')"
            ) | Out-Null
        }
        $desiredProtection = Test-DesiredClassicProtection `
            -Protection $snapshot.protection `
            -ExpectedChecks $expectedNativeChecks
        if (-not $desiredProtection) {
            $driftReasons.Add('Protection safety contract violated') | Out-Null
        }
        $driftStatus = if ($driftReasons.Count -eq 0) { 'aligned' } else { 'drift' }
    } elseif ([string]$snapshot.protectionStatus -ceq 'available-unprotected') {
        $driftMissingProtectedChecks = @($expectedNativeChecks)
        $driftReasons.Add('Default branch is unprotected') | Out-Null
        $driftStatus = 'drift'
    } else {
        $driftStatus = 'not-applicable'
    }

    if (
        $repositoryRulesetsStatus -cne 'available' -or
        $activeRules.Count -gt 0 -or
        $repositoryRulesets.Count -gt 0
    ) {
        $driftStatus = 'unverifiable'
        if ($repositoryRulesetsStatus -cne 'available') {
            $driftReasons.Add("Ruleset inspection is $repositoryRulesetsStatus") | Out-Null
        }
        if ($activeRules.Count -gt 0) {
            $driftReasons.Add("$($activeRules.Count) active default-branch rule(s) from rulesets") | Out-Null
        }
        if ($repositoryRulesets.Count -gt 0) {
            $driftReasons.Add("$($repositoryRulesets.Count) repository/inherited ruleset(s) present") | Out-Null
        }
    }
} elseif ($selectedMode -ceq 'WorkflowRun') {
    $driftStatus = 'not-applicable'
}

$legacyZeroReviewProtection = (
    [string]$snapshot.protectionStatus -ceq 'protected' -and
    (Test-LegacyZeroReviewProtection `
        -ReviewRequirement $snapshot.protection.required_pull_request_reviews)
)
$legacyConversationGate = (
    [string]$snapshot.protectionStatus -ceq 'protected' -and
    $null -eq $snapshot.protection.required_pull_request_reviews -and
    [bool]$snapshot.protection.required_conversation_resolution.enabled
)
$legacyPullRequestGate = (
    $legacyZeroReviewProtection -or
    $legacyConversationGate
)
$localLegacyMergeWorkflows = @(Get-LegacyMergeWorkflowPaths)
$remoteLegacyMergeWorkflows = @($snapshot.remoteLegacyMergeWorkflows)
$legacyMergeWorkflows = @(
    $localLegacyMergeWorkflows + $remoteLegacyMergeWorkflows |
        Sort-Object -Unique)

if ($legacyMergeWorkflows.Count -gt 0) {
    $status = 'manual'
    $nextAction = "Remove legacy PR merge automation before activation: $($legacyMergeWorkflows -join ', ')."
} elseif ($rulesetConflict) {
    $status = 'manual'
    $nextAction = 'Repository, inherited, active, disabled, or evaluate rulesets apply to the default branch. Reconcile them manually; Genesis native delivery does not mutate rulesets.'
} elseif ($selectedMode -eq 'Native' -and $missingChecks.Count -gt 0) {
    $status = 'deferred'
    $nextAction = Get-NativeMissingChecksNextAction `
        -MissingChecks $missingChecks `
        -BootstrapBlockers $bootstrapBlockers
} elseif ($selectedMode -eq 'WorkflowRun' -and -not [bool]$snapshot.privateWorkflowPresent) {
    $status = 'deferred'
    $nextAction = 'Install and merge .github/workflows/private-auto-merge.yml, then rerun.'
    if (Test-Path $installerPath) {
        if ($Apply) {
            if ($WhatIfPreference) {
                & $installerPath `
                    -ProjectRoot $ProjectRoot `
                    -WhatIf |
                    Out-Null
            } elseif ($PSCmdlet.ShouldProcess(
                $ProjectRoot,
                'Install the private exact-SHA merge workflow'
            )) {
                & $installerPath `
                    -ProjectRoot $ProjectRoot `
                    -Confirm:$false |
                    Out-Null
            }
        } else {
            & $installerPath -ProjectRoot $ProjectRoot -WhatIf | Out-Null
        }
    }
}

if ($status -ceq 'ready' -and
    [string]::IsNullOrWhiteSpace($effectiveControlRunner)) {
    $status = 'manual'
    $nextAction =
        'Rerun with -ControlRunner <approved-capability>. Use ubuntu-24.04 only for explicit hosted recovery.'
}

if ($status -eq 'ready' -and -not $AuditProtectionDrift) {
    $reviewPolicyValue =
        if ($ReviewPolicy -eq 'CopilotOneApproval') {
            'copilot-one-approval'
        } else {
            'none'
        }
    if (-not [string]::IsNullOrWhiteSpace($effectiveControlRunner)) {
        $operations.Add(
            "Set REPOSITORY_AUTOMATION_CONTROL_RUNNER=$effectiveControlRunner"
        ) | Out-Null
    }
    $operations.Add('Configure squash-only repository merge settings') | Out-Null
    $operations.Add('Set read-only Actions workflow permissions') | Out-Null
    if (-not [bool]$snapshot.private) {
        $operations.Add('Require workflow approval for all external contributors') | Out-Null
    }
    $operations.Add("Set CI_DRAFT_MODE=$draftMode") | Out-Null
    $operations.Add("Set GENESIS_REVIEW_POLICY=$reviewPolicyValue") | Out-Null
    if ($selectedMode -eq 'Native') {
        if ([string]$snapshot.protectionStatus -eq 'protected') {
            $baseProtectionIsSafe = Test-ClassicProtectionSafety `
                -Protection $snapshot.protection `
                -RequiredChecks $requiredChecks
            $reviewRequirement = $snapshot.protection.required_pull_request_reviews
            $desiredReviewState = (
                $null -eq $reviewRequirement -and
                -not [bool]$snapshot.protection.required_conversation_resolution.enabled
            )
            $compatibleProtection = (
                $baseProtectionIsSafe -and
                ($desiredReviewState -or $legacyPullRequestGate)
            )
            if (-not $compatibleProtection) {
                throw 'Existing branch protection differs from the Genesis safety contract; update it manually rather than allowing Genesis to overwrite it.'
            }
            if ($legacyPullRequestGate) {
                $operations.Add('Recreate legacy PR gate under temporary check protection') | Out-Null
            } else {
                $operations.Add('Keep existing compatible branch protection') | Out-Null
            }
        } else {
            $operations.Add('Create strict default-branch protection with required checks') | Out-Null
        }
        if ($stackedPullRequests) {
            $operations.Add('Enable native repository auto-merge for standalone pull requests') |
                Out-Null
            $operations.Add('Use GitHub-native stack checks and atomic merge or merge queue') |
                Out-Null
        } else {
            $operations.Add('Enable native repository auto-merge') | Out-Null
        }
        $operations.Add('Set GENESIS_DELIVERY_MODEL=native') | Out-Null
    } else {
        if ($stackedPullRequests) {
            $operations.Add('Use contract-validated asynchronous exact-SHA stack merge') |
                Out-Null
        }
        $operations.Add('Set GENESIS_DELIVERY_MODEL=workflow-run') | Out-Null
    }
}

$result = [ordered]@{
    schemaVersion    = 1
    repository       = [string]$snapshot.repository
    defaultBranch    = [string]$snapshot.defaultBranch
    visibility       = [string]$snapshot.visibility
    selectedMode     = $selectedMode
    status           = $status
    requiredChecks   = @($requiredChecks)
    missingChecks    = @($missingChecks)
    operations       = @($operations)
    draftMode        = $draftMode
    reviewPolicy     = $ReviewPolicy
    controlRunner    = $effectiveControlRunner
    activeRulesStatus = [string]$snapshot.activeRulesStatus
    activeRules      = @($activeRules)
    repositoryRulesetsStatus = $repositoryRulesetsStatus
    repositoryRulesets = @($repositoryRulesets)
    legacyZeroReviewProtection = $legacyZeroReviewProtection
    legacyConversationGate = $legacyConversationGate
    legacyPullRequestGate = $legacyPullRequestGate
    localLegacyMergeWorkflows = @($localLegacyMergeWorkflows)
    remoteLegacyMergeWorkflows = @($remoteLegacyMergeWorkflows)
    legacyMergeWorkflows = @($legacyMergeWorkflows)
    nextAction       = $nextAction
    protectionDrift  = [ordered]@{
        status                    = $driftStatus
        protectedChecks           = @($driftProtectedChecks)
        missingProtectedChecks    = @($driftMissingProtectedChecks)
        unexpectedProtectedChecks = @($driftUnexpectedProtectedChecks)
        reasons                   = @($driftReasons)
    }
}

if (-not $Apply -or $status -ne 'ready') {
    [PSCustomObject]$result
    return
}
if ($selectedMode -eq 'WorkflowRun' -and -not $AllowUnprotectedPrivate) {
    throw '-AllowUnprotectedPrivate is required to apply workflow-run delivery.'
}
if (-not $PSCmdlet.ShouldProcess(
    $Repository,
    "Apply Genesis $selectedMode delivery settings, variables, and local default-branch configuration"
)) {
    $result.status = if ($WhatIfPreference) { 'what-if' } else { 'cancelled' }
    [PSCustomObject]$result
    return
}

if ($selectedMode -eq 'Native') {
    $applyRulesetPages = Invoke-GhJson -Arguments @(
        'api',
        '--paginate',
        '--slurp',
        '-H', 'X-GitHub-Api-Version: 2026-03-10',
        "repos/$Repository/rulesets?includes_parents=true&targets=branch&per_page=100")
    $applyRulesets = @(
        foreach ($page in @($applyRulesetPages)) {
            @($page)
        }
    )
    if ($applyRulesets.Count -gt 0) {
        throw 'Rulesets changed after planning; rerun before applying native delivery.'
    }
    $applyProtectionStatus = 'available-unprotected'
    $applyProtection = $null
    $applyProtectionOutput = & $GhCommand api `
        -H 'X-GitHub-Api-Version: 2026-03-10' `
        "repos/$Repository/branches/$($snapshot.defaultBranch)/protection" 2>&1
    if ($LASTEXITCODE -eq 0) {
        $applyProtectionStatus = 'protected'
        $applyProtection = ($applyProtectionOutput -join "`n") |
            ConvertFrom-Json
    } elseif (($applyProtectionOutput -join "`n") -notmatch 'HTTP 404') {
        throw "Unable to revalidate classic protection before apply: $($applyProtectionOutput -join "`n")"
    }
    if ($applyProtectionStatus -cne [string]$snapshot.protectionStatus) {
        throw 'Classic protection changed after planning; rerun before applying delivery settings.'
    }
    if (
        $applyProtectionStatus -ceq 'protected' -and
        (
            ($applyProtection | ConvertTo-Json -Depth 20 -Compress) -cne
                ($snapshot.protection | ConvertTo-Json -Depth 20 -Compress)
        )
    ) {
        throw 'Classic protection changed after planning; rerun before applying delivery settings.'
    }
}

Write-Host "[1/4] Configuring repository control capability"
& $GhCommand variable set REPOSITORY_AUTOMATION_CONTROL_RUNNER `
    --repo $Repository `
    --body $effectiveControlRunner *> $null
if ($LASTEXITCODE -ne 0) {
    throw 'Failed to set REPOSITORY_AUTOMATION_CONTROL_RUNNER.'
}
$controlRunnerOutput = @(
    & $GhCommand variable get REPOSITORY_AUTOMATION_CONTROL_RUNNER `
        --repo $Repository `
        --json value `
        --jq '.value' 2>&1
)
if ($LASTEXITCODE -ne 0 -or
    ($controlRunnerOutput -join "`n").Trim() -cne
        $effectiveControlRunner) {
    throw 'Failed to verify REPOSITORY_AUTOMATION_CONTROL_RUNNER.'
}

Write-Host "[2/4] Applying squash-only repository settings"
$autoMergeValue = if ($selectedMode -eq 'Native') { 'true' } else { 'false' }
& $GhCommand api --method PATCH `
    -H 'X-GitHub-Api-Version: 2026-03-10' `
    "repos/$Repository" `
    -F allow_squash_merge=true `
    -F allow_merge_commit=false `
    -F allow_rebase_merge=false `
    -F "allow_auto_merge=$autoMergeValue" `
    -F delete_branch_on_merge=true `
    -f squash_merge_commit_title=PR_TITLE `
    -f squash_merge_commit_message=PR_BODY *> $null
if ($LASTEXITCODE -ne 0) { throw 'Failed to configure repository merge settings.' }

Write-Host "[3/4] Applying delivery model '$selectedMode'"
if ($selectedMode -eq 'Native' -and [string]$snapshot.protectionStatus -ne 'protected') {
    $checks = @(
        $requiredChecks | ForEach-Object {
            [ordered]@{ context = $_; app_id = 15368 }
        }
    )
    $protectionBody = New-ClassicProtectionBody -Checks $checks
    $protectionBody | ConvertTo-Json -Depth 8 -Compress |
        & $GhCommand api --method PUT `
            -H 'X-GitHub-Api-Version: 2026-03-10' `
            "repos/$Repository/branches/$($snapshot.defaultBranch)/protection" `
            --input - *> $null
    if ($LASTEXITCODE -ne 0) { throw 'Failed to configure branch protection.' }
    $createdProtection = Invoke-GhJson -Arguments @(
        'api',
        '-H', 'X-GitHub-Api-Version: 2026-03-10',
        "repos/$Repository/branches/$($snapshot.defaultBranch)/protection")
    if (-not (Test-DesiredClassicProtection `
        -Protection $createdProtection `
        -ExpectedChecks $checks)) {
        throw 'GitHub did not return the expected check-only branch protection.'
    }
}
if ($selectedMode -eq 'Native' -and $legacyPullRequestGate) {
    $currentRulesetPages = Invoke-GhJson -Arguments @(
        'api',
        '--paginate',
        '--slurp',
        '-H', 'X-GitHub-Api-Version: 2026-03-10',
        "repos/$Repository/rulesets?includes_parents=true&targets=branch&per_page=100")
    $currentRulesets = @(
        foreach ($page in @($currentRulesetPages)) {
            @($page)
        }
    )
    if ($currentRulesets.Count -gt 0) {
        throw 'Rulesets changed after planning; rerun before migrating branch protection.'
    }

    $currentProtection = Invoke-GhJson -Arguments @(
        'api',
        '-H', 'X-GitHub-Api-Version: 2026-03-10',
        "repos/$Repository/branches/$($snapshot.defaultBranch)/protection")
    $currentLegacyGate = (
        (Test-LegacyZeroReviewProtection `
            -ReviewRequirement $currentProtection.required_pull_request_reviews) -or
        (
            $null -eq $currentProtection.required_pull_request_reviews -and
            [bool]$currentProtection.required_conversation_resolution.enabled
        )
    )
    if (
        -not (Test-ClassicProtectionSafety `
            -Protection $currentProtection `
            -RequiredChecks $requiredChecks) -or
        -not $currentLegacyGate
    ) {
        throw 'Classic protection changed after planning; rerun before migrating it.'
    }

    $originalProtectionJson = $currentProtection |
        ConvertTo-Json -Depth 20 -Compress
    $cutoverBody = [ordered]@{
        name = 'Genesis protection cutover'
        target = 'branch'
        enforcement = 'active'
        bypass_actors = @()
        conditions = [ordered]@{
            ref_name = [ordered]@{
                include = @('~DEFAULT_BRANCH')
                exclude = @()
            }
        }
        rules = @(
            [ordered]@{
                type = 'required_status_checks'
                parameters = [ordered]@{
                    strict_required_status_checks_policy = $true
                    do_not_enforce_on_create = $false
                    required_status_checks = @(
                        $currentProtection.required_status_checks.checks |
                            ForEach-Object {
                                [ordered]@{
                                    context = [string]$_.context
                                    integration_id = [int]$_.app_id
                                }
                            }
                    )
                }
            },
            [ordered]@{ type = 'deletion' },
            [ordered]@{ type = 'non_fast_forward' }
        )
    }

    $cutoverRulesetId = $null
    $classicProtectionRemoved = $false
    $classicProtectionRestored = $false
    $classicProtectionVerified = $false
    try {
        $cutoverOutput = $cutoverBody | ConvertTo-Json -Depth 10 -Compress |
            & $GhCommand api --method POST `
                -H 'X-GitHub-Api-Version: 2026-03-10' `
                "repos/$Repository/rulesets" `
                --input -
        if ($LASTEXITCODE -ne 0) {
            throw 'Failed to create temporary cutover protection.'
        }
        $cutoverRuleset = ($cutoverOutput -join "`n") | ConvertFrom-Json
        $cutoverRulesetId = [int64]$cutoverRuleset.id
        if ($cutoverRulesetId -le 0) {
            throw 'Temporary cutover protection did not return a ruleset ID.'
        }

        $cutoverActive = $false
        $cutoverDeadline = [DateTimeOffset]::UtcNow.AddSeconds(30)
        do {
            $activeRulePages = Invoke-GhJson -Arguments @(
                'api',
                '--paginate',
                '--slurp',
                '-H', 'X-GitHub-Api-Version: 2026-03-10',
                "repos/$Repository/rules/branches/$($snapshot.defaultBranch)?per_page=100")
            $activeRulesNow = @(
                foreach ($page in @($activeRulePages)) {
                    @($page)
                }
            )
            $cutoverTypes = @(
                foreach ($rule in $activeRulesNow) {
                    if ([int64]$rule.ruleset_id -eq $cutoverRulesetId) {
                        [string]$rule.type
                    }
                }
            ) | Sort-Object
            $otherActiveRules = @(
                $activeRulesNow |
                    Where-Object {
                        [int64]$_.ruleset_id -ne $cutoverRulesetId
                    }
            )
            $cutoverActive = (
                ($cutoverTypes -join ',') -ceq
                    'deletion,non_fast_forward,required_status_checks' -and
                $otherActiveRules.Count -eq 0
            )
            if (-not $cutoverActive) {
                Start-Sleep -Seconds 1
            }
        } while (
            -not $cutoverActive -and
            [DateTimeOffset]::UtcNow -lt $cutoverDeadline
        )
        if (-not $cutoverActive) {
            throw 'Temporary cutover protection did not become active within 30 seconds.'
        }

        $beforeDelete = Invoke-GhJson -Arguments @(
            'api',
            '-H', 'X-GitHub-Api-Version: 2026-03-10',
            "repos/$Repository/branches/$($snapshot.defaultBranch)/protection")
        if (
            ($beforeDelete | ConvertTo-Json -Depth 20 -Compress) -cne
                $originalProtectionJson
        ) {
            throw 'Classic protection changed during cutover; migration was stopped.'
        }

        $deleteOutput = & $GhCommand api --method DELETE `
            -H 'X-GitHub-Api-Version: 2026-03-10' `
            "repos/$Repository/branches/$($snapshot.defaultBranch)/protection" 2>&1
        if ($LASTEXITCODE -ne 0) {
            $afterDeleteOutput = & $GhCommand api `
                -H 'X-GitHub-Api-Version: 2026-03-10' `
                "repos/$Repository/branches/$($snapshot.defaultBranch)/protection" 2>&1
            if ($LASTEXITCODE -eq 0) {
                throw "Failed to remove the legacy classic protection: $($deleteOutput -join "`n")"
            }
            if (($afterDeleteOutput -join "`n") -notmatch 'HTTP 404') {
                throw "Classic protection deletion became indeterminate: $($deleteOutput -join "`n")"
            }
        }
        $classicProtectionRemoved = $true

        $replacementBody = New-ClassicProtectionBody `
            -Checks @($currentProtection.required_status_checks.checks)
        $replacementBody | ConvertTo-Json -Depth 8 -Compress |
            & $GhCommand api --method PUT `
                -H 'X-GitHub-Api-Version: 2026-03-10' `
                "repos/$Repository/branches/$($snapshot.defaultBranch)/protection" `
                --input - *> $null
        if ($LASTEXITCODE -ne 0) {
            throw 'Failed to recreate check-only classic protection.'
        }
        $classicProtectionRestored = $true

        $replacementProtection = Invoke-GhJson -Arguments @(
            'api',
            '-H', 'X-GitHub-Api-Version: 2026-03-10',
            "repos/$Repository/branches/$($snapshot.defaultBranch)/protection")
        if (-not (Test-DesiredClassicProtection `
            -Protection $replacementProtection `
            -ExpectedChecks @($currentProtection.required_status_checks.checks))) {
            throw 'Recreated classic protection does not match the check-only contract.'
        }
        $classicProtectionVerified = $true

        & $GhCommand api --method DELETE `
            -H 'X-GitHub-Api-Version: 2026-03-10' `
            "repos/$Repository/rulesets/$cutoverRulesetId" *> $null
        if ($LASTEXITCODE -ne 0) {
            throw 'Classic protection is restored, but temporary cutover protection could not be removed.'
        }
        $cutoverRulesetId = $null
    } catch {
        if ($null -ne $cutoverRulesetId) {
            $safeToRemoveCutover = $false
            $cleanupProtectionOutput = & $GhCommand api `
                -H 'X-GitHub-Api-Version: 2026-03-10' `
                "repos/$Repository/branches/$($snapshot.defaultBranch)/protection" 2>&1
            if ($LASTEXITCODE -eq 0) {
                $cleanupProtection = ($cleanupProtectionOutput -join "`n") |
                    ConvertFrom-Json
                $safeToRemoveCutover = (
                    (Test-DesiredClassicProtection `
                        -Protection $cleanupProtection `
                        -ExpectedChecks @(
                            $currentProtection.required_status_checks.checks
                        )) -or
                    (
                        -not $classicProtectionRemoved -and
                        (
                            ($cleanupProtection | ConvertTo-Json -Depth 20 -Compress) -ceq
                                $originalProtectionJson
                        )
                    )
                )
            }
            if ($safeToRemoveCutover) {
                & $GhCommand api --method DELETE `
                    -H 'X-GitHub-Api-Version: 2026-03-10' `
                    "repos/$Repository/rulesets/$cutoverRulesetId" *> $null
            }
        }
        throw
    }
}

$workflowPermissionsBody = [ordered]@{
    default_workflow_permissions = 'read'
    can_approve_pull_request_reviews = $false
}
$workflowPermissionsBody | ConvertTo-Json -Compress |
    & $GhCommand api --method PUT `
        -H 'X-GitHub-Api-Version: 2026-03-10' `
        "repos/$Repository/actions/permissions/workflow" `
        --input - *> $null
if ($LASTEXITCODE -ne 0) { throw 'Failed to configure Actions workflow permissions.' }

if (-not [bool]$snapshot.private) {
    [ordered]@{ approval_policy = 'all_external_contributors' } |
        ConvertTo-Json -Compress |
        & $GhCommand api --method PUT `
            -H 'X-GitHub-Api-Version: 2026-03-10' `
            "repos/$Repository/actions/permissions/fork-pr-contributor-approval" `
            --input - *> $null
    if ($LASTEXITCODE -ne 0) { throw 'Failed to configure public fork workflow approval.' }
}

$modelValue = if ($selectedMode -eq 'Native') { 'native' } else { 'workflow-run' }
& $GhCommand variable set GENESIS_DELIVERY_MODEL `
    --repo $Repository `
    --body $modelValue *> $null
if ($LASTEXITCODE -ne 0) { throw 'Failed to set GENESIS_DELIVERY_MODEL.' }

& $GhCommand variable set GENESIS_REVIEW_POLICY `
    --repo $Repository `
    --body $reviewPolicyValue *> $null
if ($LASTEXITCODE -ne 0) { throw 'Failed to set GENESIS_REVIEW_POLICY.' }

& $GhCommand variable set CI_DRAFT_MODE `
    --repo $Repository `
    --body $draftMode *> $null
if ($LASTEXITCODE -ne 0) { throw 'Failed to set CI_DRAFT_MODE.' }

Write-Host "[4/4] Recording default branch locally"
git -C $ProjectRoot config genesis.defaultBranch ([string]$snapshot.defaultBranch)

$result.status = 'applied'
[PSCustomObject]$result
