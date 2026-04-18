param(
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64",
    [string]$OutputRoot = "publish\win-x64"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$outputRootPath = Join-Path $projectRoot $OutputRoot
$resourceOutput = Join-Path $outputRootPath "resource"

$env:DOTNET_CLI_HOME = Join-Path $projectRoot ".dotnet"
$env:HOME = $env:DOTNET_CLI_HOME

$projects = @(
    @{
        Name = "DiscExtract"
        Project = Join-Path $projectRoot "DiscExtract\DiscExtract.csproj"
        PublishDir = Join-Path $outputRootPath "_publish\DiscExtract"
    },
    @{
        Name = "DiscRebuild"
        Project = Join-Path $projectRoot "DiscRebuild\DiscRebuild.csproj"
        PublishDir = Join-Path $outputRootPath "_publish\DiscRebuild"
    },
    @{
        Name = "Hocotate_Toolkit"
        Project = Join-Path $projectRoot "RARCToolkit.csproj"
        PublishDir = Join-Path $outputRootPath "_publish\Hocotate_Toolkit"
    }
)

New-Item -ItemType Directory -Force -Path $outputRootPath | Out-Null
Remove-Item -Recurse -Force $resourceOutput -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $resourceOutput | Out-Null

foreach ($entry in $projects) {
    if (Test-Path $entry.PublishDir) {
        Remove-Item -Recurse -Force $entry.PublishDir
    }

    $publishArgs = @(
        "publish",
        $entry.Project,
        "-c", $Configuration,
        "-r", $RuntimeIdentifier,
        "--self-contained", "true",
        "-p:PublishSingleFile=true",
        "-p:EnableCompressionInSingleFile=true",
        "-o", $entry.PublishDir
    )

    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for $($entry.Name)."
    }
}

Copy-Item (Join-Path $projects[2].PublishDir "Hocotate_Toolkit.exe") $outputRootPath -Force
Copy-Item (Join-Path $projectRoot "README.md") $outputRootPath -Force
Copy-Item (Join-Path $projectRoot "Register_ContextMenu.bat") $outputRootPath -Force
Copy-Item (Join-Path $projectRoot "Unregister_ContextMenu.bat") $outputRootPath -Force

Get-ChildItem (Join-Path $projectRoot "resource") -File |
    Where-Object { $_.Name -notmatch '^DiscExtract(\.|$)|^DiscRebuild(\.|$)' } |
    Copy-Item -Destination $resourceOutput -Force

Copy-Item (Join-Path $projects[0].PublishDir "DiscExtract.exe") $resourceOutput -Force
Copy-Item (Join-Path $projects[1].PublishDir "DiscRebuild.exe") $resourceOutput -Force

Write-Host "Publish completed:"
Write-Host "  $outputRootPath"
