# BMD_analysis

BMD/BDL（Nintendo GameCube / Wii 向け 3D モデルフォーマット）と各種フォーマット間の相互変換ツールです．  
`Hocotate Toolkit` の `--bmd2dae` / `--bmd2obj` / `--dae2bmd` / `--fbx2bmd` で使用されます．

---

BMD_analysis is a conversion tool for BMD/BDL (Nintendo GameCube / Wii 3D model format) and various other formats.  
Used internally by Hocotate Toolkit for `--bmd2dae`, `--bmd2obj`, `--dae2bmd`, and `--fbx2bmd` operations.

## Base / ベース

SuperBMD 2.4.8.0 (by RenolY2) をベースに改編しています．  
This project is based on SuperBMD 2.4.8.0 by RenolY2.

## Changes / 変更点

- `--noskeleton` フラグを追加（スケルトンルートが見つからない FBX をスタティックメッシュとして変換）  
  Added `--noskeleton` flag to convert FBX without a `skeleton_root` node as a static mesh.
- コンソール表示名を `BMD_analysis v2` に変更  
  Console display name changed to `BMD_analysis v2`.
- BMD ファイルヘッダー識別文字列を `BMD_analysisv2.0`（16 バイト）に変更  
  BMD file header identifier string changed to `BMD_analysisv2.0` (16 bytes).

## Build / ビルド

```
dotnet build BMD_analysis.sln -c Release
```

出力: `BMD_analysis\bin\Release\net48\BMD_analysis.exe`  
Output: `BMD_analysis\bin\Release\net48\BMD_analysis.exe`
