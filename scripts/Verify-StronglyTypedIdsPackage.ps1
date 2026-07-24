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
        -Filter 'NexusLabs.StronglyTypedIds.*.nupkg'
)

if ($packages.Count -ne 1)
{
    throw "Expected exactly one NexusLabs.StronglyTypedIds package in '$resolvedPackageDirectory' but found $($packages.Count)."
}

$package = $packages[0]
$packagePrefix = 'NexusLabs.StronglyTypedIds.'
$packageVersion = $package.BaseName.Substring($packagePrefix.Length)
$repositoryRoot = Split-Path -Parent $PSScriptRoot
[xml]$packageVersions = Get-Content `
    -LiteralPath (Join-Path $repositoryRoot 'Directory.Packages.props')
$stronglyTypedIdVersion = $packageVersions.Project.ItemGroup.PackageVersion |
    Where-Object Include -EQ 'StronglyTypedId' |
    Select-Object -ExpandProperty Version -First 1

if ([string]::IsNullOrWhiteSpace($stronglyTypedIdVersion))
{
    throw 'The centrally managed StronglyTypedId package version could not be resolved.'
}

$stronglyTypedIdVersion = $stronglyTypedIdVersion.Trim('[', ']')
$archive = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)

try
{
    $documentationEntry = $archive.Entries |
        Where-Object FullName -EQ 'lib/net10.0/NexusLabs.StronglyTypedIds.xml' |
        Select-Object -First 1
    if ($null -eq $documentationEntry)
    {
        throw 'The package does not contain its IntelliSense XML documentation file.'
    }

    $reader = [System.IO.StreamReader]::new($documentationEntry.Open())
    try
    {
        $documentation = $reader.ReadToEnd()
    }
    finally
    {
        $reader.Dispose()
    }

    if (!$documentation.Contains(
            'does not enforce a UUIDv7 value invariant',
            [StringComparison]::Ordinal))
    {
        throw 'The packaged GuidIdTemplates.UuidV7 documentation is missing the arbitrary-GUID provenance warning.'
    }

    $requiredEntries = @(
        'build/AdditionalFiles/NexusLabs.UuidV7.typedid',
        'build/NexusLabs.StronglyTypedIds.targets',
        'analyzers/dotnet/cs/netstandard2.0/NexusLabs.StronglyTypedIds.Analyzers.dll',
        'analyzers/dotnet/cs/netstandard2.0/NexusLabs.StronglyTypedIds.Analyzers.CodeFixes.dll'
    )

    foreach ($requiredEntry in $requiredEntries)
    {
        $matchingEntry = $archive.Entries |
            Where-Object {
                [string]::Equals(
                    $_.FullName,
                    $requiredEntry,
                    [StringComparison]::Ordinal)
            } |
            Select-Object -First 1
        if ($null -eq $matchingEntry)
        {
            throw "The package is missing '$requiredEntry'."
        }
    }
}
finally
{
    $archive.Dispose()
}

$workRoot = Join-Path `
    ([System.IO.Path]::GetTempPath()) `
    "NexusLabs.StronglyTypedIds.PackageSmoke-$([Guid]::NewGuid().ToString('N'))"

try
{
    $packagesDirectory = Join-Path $workRoot 'packages'
    $validProjectDirectory = Join-Path $workRoot 'ValidConsumer'
    $explicitDependencyProjectDirectory =
        Join-Path $workRoot 'ExplicitDependencyConsumer'
    $analyzerProjectDirectory = Join-Path $workRoot 'AnalyzerConsumer'
    $transitiveLibraryDirectory = Join-Path $workRoot 'TransitiveLibrary'
    $transitiveConsumerDirectory = Join-Path $workRoot 'TransitiveConsumer'
    New-Item -ItemType Directory -Path $validProjectDirectory | Out-Null
    New-Item `
        -ItemType Directory `
        -Path $explicitDependencyProjectDirectory |
        Out-Null
    New-Item -ItemType Directory -Path $analyzerProjectDirectory | Out-Null
    New-Item -ItemType Directory -Path $transitiveLibraryDirectory | Out-Null
    New-Item -ItemType Directory -Path $transitiveConsumerDirectory | Out-Null

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
    <PackageReference Include="NexusLabs.StronglyTypedIds" Version="$packageVersion" />
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath $validProjectPath -Encoding UTF8

    @'
using System.Globalization;

using NexusLabs.StronglyTypedIds;

using StronglyTypedIds;

[StronglyTypedId(Template.Guid, GuidIdTemplates.UuidV7)]
public readonly partial struct OrderId;

public static class Program
{
    public static int Main()
    {
        var generated = OrderId.Create();
        var externalValue = Guid.Parse(
            "5e7d9a31-8bb4-4f82-8baa-814813645a57",
            CultureInfo.InvariantCulture);
        var rehydrated = new OrderId(externalValue);

        return generated.Value.Version == 7 &&
            rehydrated.Value == externalValue
            ? 0
            : 1;
    }
}
'@ | Set-Content `
        -LiteralPath (Join-Path $validProjectDirectory 'Program.cs') `
        -Encoding UTF8

    & dotnet restore `
        $validProjectPath `
        --configfile $nugetConfigPath `
        --packages $packagesDirectory
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

    $explicitDependencyProjectPath = Join-Path `
        $explicitDependencyProjectDirectory `
        'ExplicitDependencyConsumer.csproj'
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
    <PackageReference Include="NexusLabs.StronglyTypedIds" Version="$packageVersion" />
    <PackageReference Include="StronglyTypedId" Version="$stronglyTypedIdVersion" />
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath $explicitDependencyProjectPath -Encoding UTF8

    Copy-Item `
        -LiteralPath (Join-Path $validProjectDirectory 'Program.cs') `
        -Destination (Join-Path $explicitDependencyProjectDirectory 'Program.cs')

    & dotnet restore `
        $explicitDependencyProjectPath `
        --configfile $nugetConfigPath `
        --packages $packagesDirectory
    if ($LASTEXITCODE -ne 0)
    {
        throw 'Restoring the explicit-dependency package consumer failed.'
    }

    & dotnet run `
        --project $explicitDependencyProjectPath `
        --configuration Release `
        --no-restore
    if ($LASTEXITCODE -ne 0)
    {
        throw 'Running the explicit-dependency package consumer failed.'
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
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="NexusLabs.StronglyTypedIds" Version="$packageVersion" />
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath $analyzerProjectPath -Encoding UTF8

    @'
using NexusLabs.StronglyTypedIds;

using StronglyTypedIds;

[StronglyTypedId(Template.Guid, GuidIdTemplates.UuidV7)]
public readonly partial struct OrderId;

public static class Program
{
    public static void Main()
    {
        _ = OrderId.New();
        _ = new OrderId(Guid.NewGuid());
    }
}
'@ | Set-Content `
        -LiteralPath (Join-Path $analyzerProjectDirectory 'Program.cs') `
        -Encoding UTF8

    & dotnet restore `
        $analyzerProjectPath `
        --configfile $nugetConfigPath `
        --packages $packagesDirectory
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
        throw 'The analyzer consumer unexpectedly built without NLS0001 and NLS0002.'
    }

    foreach ($diagnosticId in @('NLS0001', 'NLS0002'))
    {
        if (!$buildOutput.Contains($diagnosticId, [StringComparison]::Ordinal))
        {
            throw "The analyzer consumer failed without reporting $diagnosticId`:`n$buildOutput"
        }
    }

    $transitiveLibraryProjectPath =
        Join-Path $transitiveLibraryDirectory 'TransitiveLibrary.csproj'
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="NexusLabs.StronglyTypedIds" Version="$packageVersion" />
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath $transitiveLibraryProjectPath -Encoding UTF8

    @'
using NexusLabs.StronglyTypedIds;

using StronglyTypedIds;

namespace PackageSmoke.Identifiers;

[StronglyTypedId(Template.Guid, GuidIdTemplates.UuidV7)]
public readonly partial struct OrderId;
'@ | Set-Content `
        -LiteralPath (Join-Path $transitiveLibraryDirectory 'OrderId.cs') `
        -Encoding UTF8

    $transitiveConsumerProjectPath =
        Join-Path $transitiveConsumerDirectory 'TransitiveConsumer.csproj'
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\TransitiveLibrary\TransitiveLibrary.csproj" />
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath $transitiveConsumerProjectPath -Encoding UTF8

    @'
using PackageSmoke.Identifiers;

_ = OrderId.New();
'@ | Set-Content `
        -LiteralPath (Join-Path $transitiveConsumerDirectory 'Program.cs') `
        -Encoding UTF8

    & dotnet restore `
        $transitiveConsumerProjectPath `
        --configfile $nugetConfigPath `
        --packages $packagesDirectory
    if ($LASTEXITCODE -ne 0)
    {
        throw 'Restoring the transitive analyzer consumer failed.'
    }

    $transitiveBuildOutput = & dotnet build `
        $transitiveConsumerProjectPath `
        --configuration Release `
        --no-restore `
        2>&1 |
        Out-String
    $transitiveBuildExitCode = $LASTEXITCODE

    if ($transitiveBuildExitCode -eq 0)
    {
        throw 'The transitive analyzer consumer unexpectedly built without NLS0001.'
    }

    if (!$transitiveBuildOutput.Contains('NLS0001', [StringComparison]::Ordinal))
    {
        throw "The transitive analyzer consumer failed without reporting NLS0001:`n$transitiveBuildOutput"
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
