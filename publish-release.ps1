param(
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64",
    [string]$OutputRoot = "publish\win-x64"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$outputRootPath = Join-Path $projectRoot $OutputRoot
$resourceOutput = Join-Path $outputRootPath "resource"
$intermediateOutput = Join-Path $outputRootPath "_publish"
$zipPath = "$outputRootPath.zip"

$env:DOTNET_CLI_HOME = Join-Path $projectRoot ".dotnet"
$env:HOME = $env:DOTNET_CLI_HOME

# ── ビルド対象プロジェクト定義 ─────────────────────────────────────────────────

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

# BMD_analysis は net48 のため dotnet build で個別ビルド（publish は別処理）
$bmdAnalysisProject   = Join-Path $projectRoot "BMD_analysis\BMD_analysis\BMD_analysis.csproj"
$bmdAnalysisPublishDir = Join-Path $outputRootPath "_publish\BMD_analysis"

# FBX_analysis は net10.0 のため dotnet publish で個別ビルド
$fbxAnalysisProject   = Join-Path $projectRoot "FBX_analysis\FbxAnalysis.csproj"
$fbxAnalysisPublishDir = Join-Path $outputRootPath "_publish\FBX_analysis"

# ── 出力フォルダ初期化 ─────────────────────────────────────────────────────────

New-Item -ItemType Directory -Force -Path $outputRootPath | Out-Null
Remove-Item -Recurse -Force $resourceOutput -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $resourceOutput | Out-Null

# ── net8.0 プロジェクトをパブリッシュ ────────────────────────────────────────

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

# ── BMD_analysis (net48) をビルド ────────────────────────────────────────────

Write-Host "Building BMD_analysis (net48)..."
if (Test-Path $bmdAnalysisPublishDir) {
    Remove-Item -Recurse -Force $bmdAnalysisPublishDir
}
New-Item -ItemType Directory -Force -Path $bmdAnalysisPublishDir | Out-Null

& dotnet build $bmdAnalysisProject -c $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed for BMD_analysis."
}

# ビルド出力から必要なファイルをコピー
$bmdBuildOut = Join-Path $projectRoot "BMD_analysis\BMD_analysis\bin\$Configuration\net48"
@("BMD_analysis.exe", "BMD_analysisLib.dll", "AssimpNet.dll", "Assimp32.dll", "Assimp64.dll",
  "Newtonsoft.Json.dll", "OpenTK.dll",
  "System.Buffers.dll", "System.Memory.dll", "System.Numerics.Vectors.dll",
  "System.Resources.Extensions.dll", "System.Runtime.CompilerServices.Unsafe.dll") |
    ForEach-Object {
        $src = Join-Path $bmdBuildOut $_
        if (Test-Path $src) {
            Copy-Item $src $bmdAnalysisPublishDir -Force
        }
    }

# ── FBX_analysis (net10.0) をパブリッシュ ────────────────────────────────────

Write-Host "Publishing FBX_analysis (net10.0)..."
if (Test-Path $fbxAnalysisPublishDir) {
    Remove-Item -Recurse -Force $fbxAnalysisPublishDir
}

& dotnet publish $fbxAnalysisProject -c $Configuration -o $fbxAnalysisPublishDir
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed for FBX_analysis."
}

# ── resource フォルダへの配置 ────────────────────────────────────────────────

# 既存 resource フォルダのファイルをコピー（DiscExtract/DiscRebuild の exe を除く）
Get-ChildItem (Join-Path $projectRoot "resource") -File |
    Where-Object { $_.Name -notmatch '^DiscExtract(\.|$)|^DiscRebuild(\.|$)|^BMD_analysis(\.|$)|^FBX_analysis(\.|$)' } |
    Copy-Item -Destination $resourceOutput -Force

# DiscExtract / DiscRebuild
Copy-Item (Join-Path $projects[0].PublishDir "DiscExtract.exe") $resourceOutput -Force
Copy-Item (Join-Path $projects[1].PublishDir "DiscRebuild.exe") $resourceOutput -Force

# BMD_analysis
Get-ChildItem $bmdAnalysisPublishDir -File | Copy-Item -Destination $resourceOutput -Force

# FBX_analysis
Get-ChildItem $fbxAnalysisPublishDir -File |
    Where-Object { $_.Extension -ne '.lib' } |  # .lib はリンカー用なので除外
    Copy-Item -Destination $resourceOutput -Force

# ── Hocotate_Toolkit.exe をルートへ配置 ──────────────────────────────────────

Copy-Item (Join-Path $projects[2].PublishDir "Hocotate_Toolkit.exe") $outputRootPath -Force
Copy-Item (Join-Path $projectRoot "README.md") $outputRootPath -Force
Copy-Item (Join-Path $projectRoot "Register_ContextMenu.bat") $outputRootPath -Force
Copy-Item (Join-Path $projectRoot "Unregister_ContextMenu.bat") $outputRootPath -Force

# ── 中間ファイル削除・ZIP 作成 ────────────────────────────────────────────────

Remove-Item -Recurse -Force $intermediateOutput -ErrorAction SilentlyContinue
Remove-Item -Force $zipPath -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $outputRootPath "*") -DestinationPath $zipPath -CompressionLevel Optimal

Write-Host "Publish completed:"
Write-Host "  $outputRootPath"
Write-Host "  $zipPath"
