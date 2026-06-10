#!/bin/sh
# Linux版 配布物作成スクリプト（Linux実機でのビルド用）
# 使い方: sh publish-release.sh
set -e

CONFIGURATION="${1:-Release}"
RID="${2:-linux-x64}"
PROJECT_ROOT="$(cd "$(dirname "$0")" && pwd)"
OUTPUT_ROOT="$PROJECT_ROOT/publish/$RID"
RESOURCE_OUTPUT="$OUTPUT_ROOT/resource"
INTERMEDIATE="$OUTPUT_ROOT/_publish"

# ── 出力フォルダ初期化 ─────────────────────────────────────────────────────────

mkdir -p "$OUTPUT_ROOT"
rm -rf "$RESOURCE_OUTPUT"
mkdir -p "$RESOURCE_OUTPUT"

# ── net8.0 プロジェクトを linux-x64 向けにパブリッシュ ───────────────────────

for NAME in DiscExtract DiscRebuild; do
    rm -rf "$INTERMEDIATE/$NAME"
    dotnet publish "$PROJECT_ROOT/$NAME/$NAME.csproj" \
        -c "$CONFIGURATION" -r "$RID" --self-contained true \
        -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true \
        -o "$INTERMEDIATE/$NAME"
done

rm -rf "$INTERMEDIATE/Hocotate_Toolkit"
dotnet publish "$PROJECT_ROOT/HocotateToolkit/HocotateToolkit.csproj" \
    -c "$CONFIGURATION" -r "$RID" --self-contained true \
    -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true \
    -o "$INTERMEDIATE/Hocotate_Toolkit"

# ── BMD_analysis (net8.0移植版) をパブリッシュ ────────────────────────────────

rm -rf "$INTERMEDIATE/BMD_analysis"
dotnet publish "$PROJECT_ROOT/BMD_analysis/BMD_analysis/BMD_analysis.csproj" \
    -c "$CONFIGURATION" -r "$RID" --self-contained true \
    -p:PublishSingleFile=true \
    -o "$INTERMEDIATE/BMD_analysis"

# ── FBX_analysis (本体リポジトリ側 net10.0 を RID 上書きでビルド) ─────────────

rm -rf "$INTERMEDIATE/FBX_analysis"
dotnet publish "$(dirname "$PROJECT_ROOT")/FBX_analysis/FbxAnalysis.csproj" \
    -c "$CONFIGURATION" -r "$RID" --self-contained true \
    -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true \
    -o "$INTERMEDIATE/FBX_analysis"

# ── 配布レイアウトへ配置 ──────────────────────────────────────────────────────
# BMD_analysis と FBX_analysis は要求する libassimp.so のバージョンが異なるため、
# それぞれ専用サブフォルダに分離して配置する（ExeRunner がサブフォルダを探索する）

cp "$INTERMEDIATE/DiscExtract/DiscExtract"  "$RESOURCE_OUTPUT/"
cp "$INTERMEDIATE/DiscRebuild/DiscRebuild"  "$RESOURCE_OUTPUT/"
cp "$(dirname "$PROJECT_ROOT")/BMD_analysis/material_presets/simpleshading.json" "$RESOURCE_OUTPUT/"

mkdir -p "$RESOURCE_OUTPUT/BMD_analysis"
cp "$INTERMEDIATE/BMD_analysis/BMD_analysis" "$RESOURCE_OUTPUT/BMD_analysis/"
cp "$INTERMEDIATE/BMD_analysis/libassimp.so" "$RESOURCE_OUTPUT/BMD_analysis/"
# libassimp.so (4.1) の依存ライブラリ（Ubuntu標準に無いため同梱。zlibライセンス）
cp "$PROJECT_ROOT/resource_linux/libminizip.so.1" "$RESOURCE_OUTPUT/BMD_analysis/"

mkdir -p "$RESOURCE_OUTPUT/FBX_analysis"
cp "$INTERMEDIATE/FBX_analysis/FBX_analysis" "$RESOURCE_OUTPUT/FBX_analysis/"
cp "$INTERMEDIATE/FBX_analysis/libassimp.so" "$RESOURCE_OUTPUT/FBX_analysis/"

cp "$INTERMEDIATE/Hocotate_Toolkit/Hocotate_Toolkit" "$OUTPUT_ROOT/"
cp "$PROJECT_ROOT/README.md" "$OUTPUT_ROOT/"

chmod +x "$OUTPUT_ROOT/Hocotate_Toolkit" "$RESOURCE_OUTPUT/DiscExtract" "$RESOURCE_OUTPUT/DiscRebuild" \
         "$RESOURCE_OUTPUT/BMD_analysis/BMD_analysis" "$RESOURCE_OUTPUT/FBX_analysis/FBX_analysis"

# ── 中間ファイル削除・tar.gz作成（実行権限を保持するためtarを使用） ──────────

rm -rf "$INTERMEDIATE"
TARBALL="$PROJECT_ROOT/publish/HocotateToolkit_$RID.tar.gz"
rm -f "$TARBALL"
tar -czf "$TARBALL" -C "$OUTPUT_ROOT" .

echo "Publish completed:"
echo "  $OUTPUT_ROOT"
echo "  $TARBALL"
