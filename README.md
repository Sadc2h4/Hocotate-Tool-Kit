# Hocotate Toolkit
<!-- .NET 8 / Windows x64 -->
![.NET](https://img.shields.io/badge/language-.NET%208-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Platform](https://img.shields.io/badge/platform-Windows%20x64-0078D4?style=flat-square&logo=windows&logoColor=white)
![Architecture](https://img.shields.io/badge/arch-x64-gray?style=flat-square)

<img width="800" height="580" alt="HokotateToolKit" src="https://github.com/user-attachments/assets/7180c3d6-cb80-4cd4-b8c8-bae866265334" />


## Download

<a href="https://github.com/Sadc2h4/Hocotate-Tool-Kit/releases/tag/v1.23c">
  <img
    src="https://raw.githubusercontent.com/Sadc2h4/brand-assets/main/button/Download_Button_1.png"
    alt="Download .zip"
    height="48"
  />
</a>
<br>
<a href="https://www.dropbox.com/scl/fi/23jpf0vj5n8qwncrjyll0/Hocotate_Toolkit_v1.23c.zip?rlkey=rajy4x31mkpvbcgc7ag5c8fyc&st=91c7d9xl&dl=0">
  <img
    src="https://raw.githubusercontent.com/Sadc2h4/brand-assets/main/button/Download_Button_4.png"
    alt="Download .zip"
    height="48"
  />
</a>
<br>

## Credits

各機能の処理で参考にしたアプリと作者は以下の通りです．  
勝手に内容を参考にしたことをお詫びするとともに，情報を共有してくださったことに感謝いたします．

----------------------------------------------------------------------------------------------------

Hocotate Toolkit itself is created by `C2H4`.  
The following applications and reference authors were consulted for implementation.

| Operation | Reference Application | Reference Author |
|--------|------|------|
| `--pack` | `RARCPack` | `Yoshi2` |
| `--szs` | `RARCPack` | `Yoshi2` |
| `--extract` | `ARCExtractor` | `cuzitsjonny` |
| `--gcextract` | `DiscExtract` | `jordan-woyak` |
| `--gcrebuild` | `DiscRebuild` | `jordan-woyak` |
| `--wiiextract` | `DiscExtract` | `jordan-woyak` |
| `--wiirebuild` | `DiscRebuild` | `jordan-woyak` |
| `--iso2wbfs` | `DiscRebuild` | `jordan-woyak` |
| `--bmd2dae` | `BMD_analysis v2` | `RenolY2` |
| `--bmd2fbx` | `FBX_analysis v2` | `MeltyPlayer` |
| `--bmd2obj` | `BMD_analysis v2` | `RenolY2` |
| `--dae2bmd` | `BMD_analysis v2` | `RenolY2` |
| `--fbx2bmd` | `BMD_analysis v2` + `simpleshading preset` | `RenolY2` |
| `--obj2grid` | `obj2grid` | `RenolY2` |


## Features

本アプリケーションはPikmin 2で使用されるNintendo GameCube / Wii向けアーカイブおよび3Dモデルフォーマットを扱う多目的コマンドラインツールです．  
**14種類の変換モード**をサポートし，**ドラッグ＆ドロップ**・**コマンドライン引数**・**Windowsの右クリックメニュー**から操作できます．
**BMD/BDLファイル**をドロップすると bmd2dae・bmd2fbx・bmd2obj の3種を一括実行し，それぞれ変換元と同じ階層に名前付きサブフォルダを作成して出力します．

----------------------------------------------------------------------------------------------------

This application is a multi-purpose command-line tool for working with Nintendo GameCube / Wii archive and 3D model formats used in Pikmin 2.  
It supports **14 conversion modes** and accepts files via **drag & drop**, **command-line arguments**, or **Windows right-click context menu**.
Dropping a **BMD/BDL** file runs all three BMD export modes at once (bmd2dae, bmd2fbx, bmd2obj) and places each output in a named subfolder next to the source file.

| Mode | Description |
|--------|------|
| `--pack` | フォルダをRARCアーカイブ (`.arc`) にパック / Pack a folder into a RARC `.arc` archive |
| `--szs` | フォルダをYaz0圧縮アーカイブ (`.szs`) にパック / Pack a folder into a Yaz0-compressed `.szs` archive|
| `--extract` | `.arc` / `.szs` アーカイブをフォルダに展開 / Extract a `.arc` or `.szs` archive to a folder |
| `--gcextract` | `.iso` / `.gcm` から GameCube ディスク全体を `files` + `sys` に抽出 / Extract a full GameCube disc from `.iso` / `.gcm` into `files` + `sys` |
| `--wiiextract` | `.iso` / `.wbfs` から Wii ディスク全体を `files` + `sys` + 追加メタデータに抽出 / Extract a full Wii disc from `.iso` / `.wbfs` into `files` + `sys` plus metadata |
| `--gcrebuild` | `sys` + `files` フォルダから GameCube ディスクを `.iso` / `.gcm` に再構築 / Rebuild a GameCube disc from `sys` + `files` into `.iso` / `.gcm` |
| `--wiirebuild` | `sys` + `files` + Wii メタデータから Wii ディスクを `.iso` / `.wbfs` に再構築 / Rebuild a Wii disc from `sys` + `files` + metadata into `.iso` / `.wbfs` |
| `--iso2wbfs` | `.iso` を `.wbfs` に変換 / Convert `.iso` to `.wbfs` |
| `--bmd2dae` | `.bmd` / `.bdl` をCollada形式 `.dae` に変換 (BMD_analysis使用) / Convert `.bmd` / `.bdl` to Collada `.dae` (via BMD_analysis)|
| `--bmd2fbx` | `.bmd` / `.bdl` を ASCII形式 `.fbx` + `.glb` に変換 (FBX_analysis v2使用) / Convert `.bmd` / `.bdl` to ASCII `.fbx` + `.glb` (via FBX_analysis v2) |
| `--bmd2obj` | `.bmd` / `.bdl` を `.obj` + `.mtl` に変換 (BMD_analysis使用)/ Convert `.bmd` / `.bdl` to `.obj` + `.mtl` (via BMD_analysis) |
| `--dae2bmd` | Collada形式 `.dae` を `.bmd` に変換 (BMD_analysis使用) / Convert Collada `.dae` back to `.bmd` (via BMD_analysis) |
| `--fbx2bmd` | `.fbx` を `.bmd` に変換 (`BMD_analysis v2` + `simpleshading.json` 使用) / Convert `.fbx` to `.bmd` (via `BMD_analysis v2` + `simpleshading.json`) |
| `--obj2grid` | `.obj` コリジョンメッシュをPikmin 2の `grid.bin` + `mapcode.bin` に変換 /  Convert `.obj` collision mesh to Pikmin 2 `grid.bin` + `mapcode.bin`  |


## Setup

以下のファイルを同一フォルダにまとめて配置してください．  
`Hocotate_Toolkit.exe` は自己完結型のシングルファイルバイナリのため，.NET のインストールは不要です．  
`resource\` フォルダは必ず exe と同じ階層に置いてください．
GameCube / Wii ディスクの抽出機能、GameCube / Wii ディスク再構築機能、`--iso2wbfs` を使う場合は `DiscExtract.exe` と `DiscRebuild.exe` も `resource\` に配置してください．

----------------------------------------------------------------------------------------------------

Place the following files together in one folder.  
`Hocotate_Toolkit.exe` is a self-contained single-file binary; no .NET installation is required.  
The `resource\` folder must stay in the same directory as the exe.
For the GameCube / Wii disc extraction feature, the GameCube / Wii disc rebuild feature, and `--iso2wbfs`, also place `DiscExtract.exe` and `DiscRebuild.exe` inside `resource\`.
For `--fbx2bmd`, also place `simpleshading.json` inside `resource\`.
`BMD_analysis.exe` is used by `--bmd2dae`, `--dae2bmd`, `--bmd2obj`, and `--fbx2bmd`.

```
Hocotate_Toolkit.exe
Register_ContextMenu.bat
Unregister_ContextMenu.bat
resource\
    DiscExtract.exe
    DiscRebuild.exe
    BMD_analysis.exe
    FBX_analysis.exe
    AssimpNet.dll
    Assimp32.dll
    Assimp64.dll
    SuperBMDLib.dll
    Newtonsoft.Json.dll
    OpenTK.dll
    EndianBinaryStreams.dll
    GameFormatReader.dll
    simpleshading.json
    (+ other required DLLs)
```

## Usage

### Drag & Drop

対応ファイルまたはフォルダを `Hocotate_Toolkit.exe` に直接ドラッグ＆ドロップすると，入力の種類を自動判別して適切な変換を実行します．  
出力は変換元ファイルと同じ階層に，変換元と同名のサブフォルダを作成して格納されます．

----------------------------------------------------------------------------------------------------

Drag any supported file or folder directly onto `Hocotate_Toolkit.exe`.  
The tool auto-detects the input type and runs the appropriate conversion.  
Output is placed in a subfolder named after the input file, at the same directory level as the input.

| ドロップ対象 / Dropped item | 実行される処理 / Action |
|-----------------------------|-------------------------|
| フォルダ / Folder | `--szs` / `--gcrebuild` / `--wiirebuild` (`sys` + `files` があればGC/Wiiディスク再構築 / rebuild GC/Wii disc if `sys` + `files` exist) |
| `.arc` / `.szs` | `--extract` (展開 / Extract) |
| `.iso` / `.gcm` | `--gcextract` / `--wiiextract` 自動判別 (GC/Wii ディスク全体抽出 / full disc extract with GC/Wii auto-detect) |
| `.wbfs` | `--wiiextract` (Wii ディスク全体抽出 / full Wii disc extract) |
| `.bmd` / `.bdl` | bmd2dae + bmd2fbx + bmd2obj (3種一括 / all three) |
| `.dae` | `--dae2bmd` |
| `.fbx` | `--fbx2bmd` |
| `.obj` | `--obj2grid` |

`--register` を実行すると、`.iso` の右クリックメニューに `Convert ISO to WBFS` が追加され、`.fbx` には `FBX to BMD`、フォルダ右クリックには `Pack to SZS` / `Rebuild GC Disc` / `Rebuild Wii Disc` が個別に追加されます。

### Command Line / コマンドライン
<img width="600" height="350" alt="image" src="https://github.com/user-attachments/assets/ed6b1850-b27c-4907-97b3-b96f29b6d657" />

```
Hocotate_Toolkit.exe --<mode> <input> [output]
```

出力パスを省略した場合，変換元ファイルと同じ階層に同名のサブフォルダが自動生成され，そこに結果が書き出されます．  
When an output path is omitted, the tool creates a subfolder named after the input file and writes results there.

```bat
:: フォルダを .arc にパック / Pack a folder into .arc
Hocotate_Toolkit.exe --pack "C:\path\to\folder"

:: フォルダを .szs (Yaz0) にパック / Pack a folder into .szs (Yaz0)
Hocotate_Toolkit.exe --szs "C:\path\to\folder"

:: .arc / .szs を展開 / Extract .arc or .szs
Hocotate_Toolkit.exe --extract "C:\path\to\file.szs"

:: GameCube ISO/GCM を files + sys に展開 / Extract GameCube ISO/GCM to files + sys
Hocotate_Toolkit.exe --gcextract "C:\path\to\game.iso"

:: Wii ISO/WBFS を files + sys + metadata に展開 / Extract Wii ISO/WBFS to files + sys + metadata
Hocotate_Toolkit.exe --wiiextract "C:\path\to\game.wbfs"

:: sys + files から GameCube ISO を再構築 / Rebuild GameCube ISO from sys + files
Hocotate_Toolkit.exe --gcrebuild "C:\path\to\game_folder"

:: sys + files + metadata から Wii ISO/WBFS を再構築 / Rebuild Wii ISO/WBFS from sys + files + metadata
Hocotate_Toolkit.exe --wiirebuild "C:\path\to\wii_game_folder"

:: ISO を WBFS に変換 / Convert ISO to WBFS
Hocotate_Toolkit.exe --iso2wbfs "C:\path\to\game.iso"

:: BMD を Collada に変換 / Convert BMD to Collada
Hocotate_Toolkit.exe --bmd2dae "C:\path\to\model.bmd"

:: BMD を FBX に変換 / Convert BMD to FBX
Hocotate_Toolkit.exe --bmd2fbx "C:\path\to\model.bmd"

:: BMD を OBJ に変換 / Convert BMD to OBJ
Hocotate_Toolkit.exe --bmd2obj "C:\path\to\model.bmd"

:: Collada を BMD に変換 / Convert Collada to BMD
Hocotate_Toolkit.exe --dae2bmd "C:\path\to\model.dae" "C:\path\to\out.bmd" --mat "C:\path\to\model_materials.json" --texheader "C:\path\to\model_tex_headers.json"

:: SuperBMD.exe "%~1" --mat "%~dp1\%~n1_materials.json" --texheader "%~dp1\%~n1_tex_headers.json" と同等の指定例
:: Example equivalent to SuperBMD.exe "%~1" --mat "%~dp1\%~n1_materials.json" --texheader "%~dp1\%~n1_tex_headers.json"
Hocotate_Toolkit.exe --dae2bmd "C:\path\to\model.dae" "C:\path\to\out.bmd" --mat "C:\path\to\model_materials.json" --texheader "C:\path\to\model_tex_headers.json"

:: FBX を BMD に変換 / Convert FBX to BMD
Hocotate_Toolkit.exe --fbx2bmd "C:\path\to\model.fbx"

:: OBJ コリジョンメッシュを grid.bin に変換 / Convert OBJ collision mesh to grid.bin
Hocotate_Toolkit.exe --obj2grid "C:\path\to\collision.obj"
```

### GameCube Round-Trip Notes
<img width="500" height="350" alt="HokotateToolKit2" src="https://github.com/user-attachments/assets/006d1a4e-ca04-423c-9d29-f8a5cede1394" />

実ロムを用いた往復検証では，再構築した `.iso` が Dolphin で認識され，再抽出した `files` の内容も元と一致しました．  
ただし，再構築後のイメージは元ROMと完全なバイナリ一致にはなりません．

----------------------------------------------------------------------------------------------------

In round-trip verification with a real GameCube ISO, the rebuilt image was recognized by Dolphin, and
the re-extracted `files` contents matched the original. However, the rebuilt image is not expected to
be byte-identical to the source disc.

- Rebuilt images may differ in total ISO/GCM file size.
- `sys\boot.bin` may differ because DOL/FST/user area offsets are rewritten.
- `sys\fst.bin` may differ because the file system table is regenerated during rebuild.

### Wii Round-Trip Notes

<img width="500" height="300" alt="HokotateToolKit3" src="https://github.com/user-attachments/assets/79bf8bdd-37af-4ed5-bd63-d5843edbf08a" />

Wii 再構築では `ticket.bin`・`tmd.bin`・`cert.bin`・`disc\header.bin`・`disc\region.bin` を使ってゲームパーティションを再構成し，出力拡張子に応じて `.iso` または `.wbfs` を生成します．  

----------------------------------------------------------------------------------------------------
Wii rebuild uses `ticket.bin`, `tmd.bin`, `cert.bin`, `disc\header.bin`, and `disc\region.bin` to reconstruct the game partition and outputs either `.iso` or `.wbfs` depending on the requested extension.

### FBX Conversion Notes / FBX 変換について

**`--bmd2fbx` (BMD → FBX)**  
v1.23c より `FBX_analysis v2`（MeltyTool / Assimp 5.x ベース）に更新されました．

- 出力 FBX の形式が **バイナリから ASCII 形式（FBX 7.5.0）** に変更されました．
  - ASCII FBX は `--fbx2bmd` で使用する `BMD_analysis v2` 内部の Assimp 3.x と互換性があります．
- FBX と同時に **`モデル名_gltf.glb`** ファイルも出力されます（Blender 等に直接インポート可能）．
- FBX 内部に `skeleton_root` ダミーノードが含まれ，`--fbx2bmd` での再変換時にボーン情報が保持されます．

**`--fbx2bmd` (FBX → BMD)**  
v1.23c より `BMD_analysis v2`（SuperBMD 再ビルド版）に更新されました．

- FBX に `skeleton_root` ノードが存在しない場合，スタティックメッシュ（骨格なし）として変換します．
- BMD → FBX → BMD の往復変換では，元のファイルよりサイズが増加します（ゲーム内動作への影響はありません）．
  - UV シームでの頂点分裂（FBX の per-vertex 形式への変換で発生）
  - 頂点データ精度の変化（固定小数点 → Float32）
  - トライストリップ効率の差（アルゴリズム再生成のため）

----------------------------------------------------------------------------------------------------

**`--bmd2fbx` (BMD → FBX)**  
Updated to `FBX_analysis v2` (based on MeltyTool / Assimp 5.x) from v1.23c.

- The FBX output format changed from binary to **ASCII (FBX 7.5.0)**.
  - ASCII FBX is compatible with Assimp 3.x inside `BMD_analysis v2` used by `--fbx2bmd`.
- A **`modelname_gltf.glb`** file is also exported alongside the FBX (importable directly in Blender etc.).
- The FBX includes a `skeleton_root` dummy node so that bone data is preserved when converting back with `--fbx2bmd`.

**`--fbx2bmd` (FBX → BMD)**  
Updated to `BMD_analysis v2` (rebuilt SuperBMD) from v1.23c.

- If the FBX does not contain a `skeleton_root` node, it is converted as a static mesh (no skeleton).
- Round-trip BMD → FBX → BMD conversion results in a larger output file than the original (no effect on in-game behavior).
  - Vertex splitting at UV seams (caused by conversion to FBX's per-vertex attribute format)
  - Increased vertex data precision (fixed-point → Float32)
  - Tristrip efficiency differences (algorithmically regenerated strips)

### Context Menu / 右クリックメニュー連携

`Register_ContextMenu.bat` を実行すると，`.arc`・`.szs`・`.iso`・`.gcm`・`.wbfs`・`.bmd`・`.bdl`・`.dae`・`.fbx`・`.obj` とフォルダに用途別の右クリック項目が追加されます．
管理者権限は不要です（ユーザー単位のレジストリ HKCU に登録されます）．

----------------------------------------------------------------------------------------------------

Run `Register_ContextMenu.bat` to add purpose-specific Hocotate Toolkit entries to the Windows right-click menu for supported file types (`.arc`, `.szs`, `.iso`, `.gcm`, `.wbfs`, `.bmd`, `.bdl`, `.dae`, `.fbx`, `.obj`) and folders.
No administrator rights are required — entries are registered per-user (HKCU).

```bat
:: 右クリックメニューに追加 / Add context menu entries
Register_ContextMenu.bat

:: 右クリックメニューから削除 / Remove context menu entries
Unregister_ContextMenu.bat
```

フォルダ右クリックでは以下の3項目が個別に登録されます。
`Hocotate Toolkit - Pack to SZS`
`Hocotate Toolkit - Rebuild GC Disc`
`Hocotate Toolkit - Rebuild Wii Disc`

The folder context menu now registers these three separate entries:
`Hocotate Toolkit - Pack to SZS`
`Hocotate Toolkit - Rebuild GC Disc`
`Hocotate Toolkit - Rebuild Wii Disc`

> [!IMPORTANT]
> **過去バージョンのメニューが残っている場合 / Removing leftover entries from older versions:**  
> 以前のバージョンで登録したメニューが右クリックに残り続けている場合は，一度 `Unregister_ContextMenu.bat` を実行して古いエントリをすべて削除してから，改めて `Register_ContextMenu.bat` で再登録してください．  
> アンレジストレーションはラベル名・アイコンパスを検索して過去バージョンのキー名も含めて網羅的に削除するため，旧バージョンのメニューも正しく除去されます．  
>  
> If context menu entries from a previous version remain in the right-click menu, run `Unregister_ContextMenu.bat` first to remove all leftover entries, then run `Register_ContextMenu.bat` again to re-register cleanly.  
> The unregistration searches by label name and icon path, covering old version key names as well, so entries from any prior version are correctly removed.

> [!IMPORTANT]
> **注意 / Note:** Windows 11 では **"その他のオプションを確認"** をクリックして表示される旧右クリックメニュー内に項目が表示されます．  
> On Windows 11, the Hocotate Toolkit entries appear under **"Show more options"** (the legacy context menu).

## Deletion Method / 削除方法

・`Hocotate_Toolkit.exe` が入ったフォルダごと削除してください．  
　右クリックメニューを登録している場合は，先に `Unregister_ContextMenu.bat` を実行してレジストリ登録を解除してください．

----------------------------------------------------------------------------------------------------

・Please delete the entire folder containing `Hocotate_Toolkit.exe`.  
　If you registered the context menu, run `Unregister_ContextMenu.bat` first to remove the registry entries.

## Disclaimer / 免責事項

・本ソフトウェアの使用によって生じたいかなる損害についても，作者は一切の責任を負いません．  
・I assume no responsibility whatsoever for any damages incurred through the use of this software.
