# Hocotate Toolkit
<!-- .NET 8 / Windows x64 -->
![.NET](https://img.shields.io/badge/language-.NET%208-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Platform](https://img.shields.io/badge/platform-Windows%20x64-0078D4?style=flat-square&logo=windows&logoColor=white)
![Architecture](https://img.shields.io/badge/arch-x64-gray?style=flat-square)



## Download

*(Release package coming soon)*

## Features

本アプリケーションはPikmin 2で使用されるNintendo GameCube / Wii向けアーカイブおよび3Dモデルフォーマットを扱う多目的コマンドラインツールです．  
**8種類の変換モード**をサポートし，**ドラッグ＆ドロップ**・**コマンドライン引数**・**Windowsの右クリックメニュー**から操作できます．

**BMD/BDLファイル**をドロップすると bmd2dae・bmd2fbx・bmd2obj の3種を一括実行し，それぞれ変換元と同じ階層に名前付きサブフォルダを作成して出力します．

----------------------------------------------------------------------------------------------------

This application is a multi-purpose command-line tool for working with Nintendo GameCube / Wii archive and 3D model formats used in Pikmin 2.  
It supports **8 conversion modes** and accepts files via **drag & drop**, **command-line arguments**, or **Windows right-click context menu**.

Dropping a **BMD/BDL** file runs all three BMD export modes at once (bmd2dae, bmd2fbx, bmd2obj) and places each output in a named subfolder next to the source file.

| Mode | Description |
|------|-------------|
| `--pack` | Pack a folder into a RARC `.arc` archive |
| `--szs` | Pack a folder into a Yaz0-compressed `.szs` archive |
| `--extract` | Extract a `.arc` or `.szs` archive to a folder |
| `--bmd2dae` | Convert `.bmd` / `.bdl` to Collada `.dae` (via BMD_analysis) |
| `--bmd2fbx` | Convert `.bmd` / `.bdl` to `.fbx` (via FBX_analysis) |
| `--bmd2obj` | Convert `.bmd` / `.bdl` to `.obj` + `.mtl` (via BMD_analysis) |
| `--dae2bmd` | Convert Collada `.dae` back to `.bmd` (via BMD_analysis) |
| `--obj2grid` | Convert `.obj` collision mesh to Pikmin 2 `grid.bin` + `mapcode.bin` |



## Setup

以下のファイルを同一フォルダにまとめて配置してください．  
`Hocotate_Toolkit.exe` は自己完結型のシングルファイルバイナリのため，.NET のインストールは不要です．  
`resource\` フォルダは必ず exe と同じ階層に置いてください．

----------------------------------------------------------------------------------------------------

Place the following files together in one folder.  
`Hocotate_Toolkit.exe` is a self-contained single-file binary; no .NET installation is required.  
The `resource\` folder must stay in the same directory as the exe.

```
Hocotate_Toolkit.exe
Register_ContextMenu.bat
Unregister_ContextMenu.bat
resource\
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
| フォルダ / Folder | `--szs` (Yaz0圧縮アーカイブ / Yaz0-compressed archive) |
| `.arc` / `.szs` | `--extract` (展開 / Extract) |
| `.bmd` / `.bdl` | bmd2dae + bmd2fbx + bmd2obj (3種一括 / all three) |
| `.dae` | `--dae2bmd` |
| `.obj` | `--obj2grid` |

### Command Line / コマンドライン

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

:: BMD を Collada に変換 / Convert BMD to Collada
Hocotate_Toolkit.exe --bmd2dae "C:\path\to\model.bmd"

:: BMD を FBX に変換 / Convert BMD to FBX
Hocotate_Toolkit.exe --bmd2fbx "C:\path\to\model.bmd"

:: BMD を OBJ に変換 / Convert BMD to OBJ
Hocotate_Toolkit.exe --bmd2obj "C:\path\to\model.bmd"

:: Collada を BMD に変換 / Convert Collada to BMD
Hocotate_Toolkit.exe --dae2bmd "C:\path\to\model.dae" "C:\path\to\out.bmd" --mat "C:\path\to\materials.json"

:: OBJ コリジョンメッシュを grid.bin に変換 / Convert OBJ collision mesh to grid.bin
Hocotate_Toolkit.exe --obj2grid "C:\path\to\collision.obj"
```

### Context Menu / 右クリックメニュー連携

`Register_ContextMenu.bat` を実行すると，`.arc`・`.szs`・`.bmd`・`.bdl`・`.dae`・`.obj` およびフォルダの右クリックメニューに **"Hocotate Toolkit"** の項目が追加されます．  
管理者権限は不要です（ユーザー単位のレジストリ HKCU に登録されます）．

----------------------------------------------------------------------------------------------------

Run `Register_ContextMenu.bat` to add **"Hocotate Toolkit"** entries to the Windows right-click menu for supported file types (`.arc`, `.szs`, `.bmd`, `.bdl`, `.dae`, `.obj`) and folders.  
No administrator rights are required — entries are registered per-user (HKCU).

```bat
:: 右クリックメニューに追加 / Add context menu entries
Register_ContextMenu.bat

:: 右クリックメニューから削除 / Remove context menu entries
Unregister_ContextMenu.bat
```

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
