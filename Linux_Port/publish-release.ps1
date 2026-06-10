param(
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "linux-x64",
    [string]$OutputRoot = "publish\linux-x64"
)

# Linux版 配布物作成スクリプト（Windows上からのクロスビルド用）
# Linux実機でビルドする場合は publish-release.sh を使用してください。

$ErrorActionPreference = "Stop"

$projectRoot = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
if ([string]::IsNullOrWhiteSpace($projectRoot)) {
    $projectRoot = (Get-Location).Path
}
$outputRootPath = Join-Path $projectRoot $OutputRoot
$resourceOutput = Join-Path $outputRootPath "resource"
$intermediateOutput = Join-Path $outputRootPath "_publish"
$zipPath = "$outputRootPath.zip"

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
        Project = Join-Path $projectRoot "HocotateToolkit\HocotateToolkit.csproj"
        PublishDir = Join-Path $outputRootPath "_publish\Hocotate_Toolkit"
    }
)

# ── 出力フォルダ初期化 ─────────────────────────────────────────────────────────

New-Item -ItemType Directory -Force -Path $outputRootPath | Out-Null
Remove-Item -Recurse -Force $resourceOutput -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $resourceOutput | Out-Null

# ── net8.0 プロジェクトを linux-x64 向けにパブリッシュ ───────────────────────

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

# ── BMD_analysis (net8.0移植版) をパブリッシュ ────────────────────────────────

$bmdPublishDir = Join-Path $outputRootPath "_publish\BMD_analysis"
if (Test-Path $bmdPublishDir) {
    Remove-Item -Recurse -Force $bmdPublishDir
}
& dotnet publish (Join-Path $projectRoot "BMD_analysis\BMD_analysis\BMD_analysis.csproj") `
    -c $Configuration -r $RuntimeIdentifier --self-contained true `
    -p:PublishSingleFile=true -o $bmdPublishDir
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed for BMD_analysis."
}

# ── FBX_analysis (本体リポジトリ側 net10.0 を RID 上書きでクロスビルド) ───────

$fbxPublishDir = Join-Path $outputRootPath "_publish\FBX_analysis"
$fbxProject = Join-Path (Split-Path $projectRoot -Parent) "FBX_analysis\FbxAnalysis.csproj"
if (Test-Path $fbxPublishDir) {
    Remove-Item -Recurse -Force $fbxPublishDir
}
& dotnet publish $fbxProject `
    -c $Configuration -r $RuntimeIdentifier --self-contained true `
    -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o $fbxPublishDir
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed for FBX_analysis."
}

# ── resource フォルダへの配置（Linuxバイナリは拡張子なし） ────────────────────
# BMD_analysis と FBX_analysis は要求する libassimp.so のバージョンが異なるため、
# それぞれ専用サブフォルダに分離して配置する（ExeRunner がサブフォルダを探索する）

Copy-Item (Join-Path $projects[0].PublishDir "DiscExtract") $resourceOutput -Force
Copy-Item (Join-Path $projects[1].PublishDir "DiscRebuild") $resourceOutput -Force
Copy-Item (Join-Path (Split-Path $projectRoot -Parent) "BMD_analysis\material_presets\simpleshading.json") $resourceOutput -Force

$bmdResourceDir = Join-Path $resourceOutput "BMD_analysis"
New-Item -ItemType Directory -Force -Path $bmdResourceDir | Out-Null
Copy-Item (Join-Path $bmdPublishDir "BMD_analysis") $bmdResourceDir -Force
Copy-Item (Join-Path $bmdPublishDir "libassimp.so") $bmdResourceDir -Force
# libassimp.so (4.1) の依存ライブラリ（Ubuntu標準に無いため同梱。zlibライセンス）
Copy-Item (Join-Path $projectRoot "resource_linux\libminizip.so.1") $bmdResourceDir -Force

$fbxResourceDir = Join-Path $resourceOutput "FBX_analysis"
New-Item -ItemType Directory -Force -Path $fbxResourceDir | Out-Null
Copy-Item (Join-Path $fbxPublishDir "FBX_analysis") $fbxResourceDir -Force
Copy-Item (Join-Path $fbxPublishDir "libassimp.so") $fbxResourceDir -Force

# ── Hocotate_Toolkit をルートへ配置 ──────────────────────────────────────────

Copy-Item (Join-Path $projects[2].PublishDir "Hocotate_Toolkit") $outputRootPath -Force
Copy-Item (Join-Path $projectRoot "README.md") $outputRootPath -Force

# ── 中間ファイル削除・アーカイブ作成 ──────────────────────────────────────────
# 注意: zip経由では実行権限が保持されないため、展開後に chmod +x Hocotate_Toolkit が必要
# （resource/ 内のツールは本体が起動時に実行権限を自動付与する）

Remove-Item -Recurse -Force $intermediateOutput -ErrorAction SilentlyContinue
Remove-Item -Force $zipPath -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $outputRootPath "*") -DestinationPath $zipPath -CompressionLevel Optimal

Write-Host "Publish completed:"
Write-Host "  $outputRootPath"
Write-Host "  $zipPath"
Write-Host ""
Write-Host "After extracting on Linux, run: chmod +x Hocotate_Toolkit"
