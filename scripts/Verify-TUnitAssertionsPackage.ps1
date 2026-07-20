[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackageDirectory
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

$resolvedPackageDirectory = (Resolve-Path -LiteralPath $PackageDirectory).Path
$packages = @(
    Get-ChildItem `
    -LiteralPath $resolvedPackageDirectory `
    -Filter 'NexusLabs.TUnit.Assertions.*.nupkg'
)

if ($packages.Count -ne 1)
{
    throw "Expected exactly one NexusLabs.TUnit.Assertions package in '$resolvedPackageDirectory' but found $($packages.Count)."
}

$package = $packages[0]
$packagePrefix = 'NexusLabs.TUnit.Assertions.'
$packageVersion = $package.BaseName.Substring($packagePrefix.Length)
$repositoryRoot = Split-Path -Parent $PSScriptRoot
[xml]$packageVersions = Get-Content `
    -LiteralPath (Join-Path $repositoryRoot 'Directory.Packages.props')
$tunitVersion = $packageVersions.Project.ItemGroup.PackageVersion |
    Where-Object Include -EQ 'TUnit' |
    Select-Object -ExpandProperty Version -First 1

if ([string]::IsNullOrWhiteSpace($tunitVersion))
{
    throw 'The centrally managed TUnit package version could not be resolved.'
}

$workRoot = Join-Path `
    ([System.IO.Path]::GetTempPath()) `
    "NexusLabs.TUnit.Assertions.PackageSmoke-$([Guid]::NewGuid().ToString('N'))"

try
{
    $validProjectDirectory = Join-Path $workRoot 'ValidConsumer'
    $analyzerProjectDirectory = Join-Path $workRoot 'AnalyzerConsumer'
    New-Item -ItemType Directory -Path $validProjectDirectory | Out-Null
    New-Item -ItemType Directory -Path $analyzerProjectDirectory | Out-Null

    $escapedPackageDirectory =
        [System.Security.SecurityElement]::Escape($resolvedPackageDirectory)
    $nugetConfigPath = Join-Path $workRoot 'NuGet.config'
    @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$escapedPackageDirectory" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@ | Set-Content -LiteralPath $nugetConfigPath -Encoding UTF8

    $validProjectPath = Join-Path $validProjectDirectory 'ValidConsumer.csproj'
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="NexusLabs.TUnit.Assertions" Version="$packageVersion" />
    <PackageReference Include="TUnit" Version="$tunitVersion" />
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath $validProjectPath -Encoding UTF8

    @'
using NexusLabs.Framework;
using NexusLabs.TUnit.Assertions;

public sealed class PackageSmokeTests
{
    [Test]
    public async Task Assertions_ReturnValuesAndErrors(
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        TriedEx<int> success = 42;
        TriedEx<int> failure = new ArgumentException("expected");

        var value = await Assert.That(success)
            .Succeeded()
            .Because("The successful result should expose its value");
        var error = await Assert.That(failure)
            .Failed()
            .With<ArgumentException>()
            .Because("The failed result should expose its exception");

        await Assert.That(value).IsEqualTo(42);
        await Assert.That(error.Message).IsEqualTo("expected");
    }
}
'@ | Set-Content `
        -LiteralPath (Join-Path $validProjectDirectory 'PackageSmokeTests.cs') `
        -Encoding UTF8

    & dotnet restore $validProjectPath --configfile $nugetConfigPath
    if ($LASTEXITCODE -ne 0)
    {
        throw 'Restoring the valid package consumer failed.'
    }

    & dotnet run `
        --project $validProjectPath `
        --configuration Release `
        --no-restore
    if ($LASTEXITCODE -ne 0)
    {
        throw 'Running the valid package consumer failed.'
    }

    $analyzerProjectPath =
        Join-Path $analyzerProjectDirectory 'AnalyzerConsumer.csproj'
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <WarningsAsErrors>NLT0001</WarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="NexusLabs.TUnit.Assertions" Version="$packageVersion" />
    <PackageReference Include="TUnit" Version="$tunitVersion" />
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath $analyzerProjectPath -Encoding UTF8

    @'
using NexusLabs.Framework;

public sealed class AnalyzerPackageSmokeTests
{
    [Test]
    public async Task DirectMemberAssertion_IsRejected(
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        TriedEx<int> result = 42;

        await Assert.That(result.Success)
            .IsTrue()
            .Because("The package analyzer should reject this pattern");
    }
}
'@ | Set-Content `
        -LiteralPath (Join-Path $analyzerProjectDirectory 'AnalyzerPackageSmokeTests.cs') `
        -Encoding UTF8

    & dotnet restore $analyzerProjectPath --configfile $nugetConfigPath
    if ($LASTEXITCODE -ne 0)
    {
        throw 'Restoring the analyzer package consumer failed.'
    }

    $buildOutput = & dotnet build `
        $analyzerProjectPath `
        --configuration Release `
        --no-restore `
        2>&1 |
        Out-String
    $buildExitCode = $LASTEXITCODE

    if ($buildExitCode -eq 0)
    {
        throw 'The analyzer consumer unexpectedly built without NLT0001.'
    }

    if (!$buildOutput.Contains('NLT0001', [StringComparison]::Ordinal))
    {
        throw "The analyzer consumer failed without reporting NLT0001:`n$buildOutput"
    }
}
finally
{
    if (Test-Path -LiteralPath $workRoot)
    {
        Remove-Item -LiteralPath $workRoot -Recurse -Force
    }
}

$global:LASTEXITCODE = 0
