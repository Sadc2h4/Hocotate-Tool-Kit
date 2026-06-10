# Hocotate Toolkit (Linux)
<!-- .NET 8 / Linux x64 -->
![.NET](https://img.shields.io/badge/language-.NET%208-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Platform](https://img.shields.io/badge/platform-Linux%20x64-FCC624?style=flat-square&logo=linux&logoColor=black)
![Architecture](https://img.shields.io/badge/arch-x64-gray?style=flat-square)

Windows版 Hocotate Toolkit の Linux 移植版です．  
This is the Linux port of Hocotate Toolkit.

## Credits

Hocotate Toolkit itself is created by `C2H4`.  
各機能の処理で参考にしたアプリと作者は Windows 版 README を参照してください．  
See the Windows README for the reference applications and authors consulted for each feature.

## Supported Features / 対応機能

Windows版の**全16モード**に対応しています．  
モデル変換系の外部ツールも Linux 向けに移植済みです（BMD_analysis は .NET 8 へ移植，FBX_analysis は linux-x64 ビルド）．

All **16 modes** of the Windows build are supported.
The model conversion tools are also ported to Linux (BMD_analysis migrated to .NET 8, FBX_analysis built for linux-x64).

| Mode | Description | Linux |
|--------|------|:---:|
| `--pack` | フォルダをRARCアーカイブ (`.arc`) にパック / Pack a folder into `.arc` | ✅ |
| `--szs` | フォルダをYaz0圧縮アーカイブ (`.szs`) にパック / Pack a folder into `.szs` | ✅ |
| `--extract` | `.arc` / `.szs` をフォルダに展開 / Extract `.arc` / `.szs` | ✅ |
| `--gcextract` | GameCubeディスクを `files` + `sys` に抽出 / Extract a GameCube disc | ✅ |
| `--wiiextract` | Wiiディスクを `files` + `sys` + メタデータに抽出 / Extract a Wii disc | ✅ |
| `--gcrebuild` | GameCubeディスクを再構築 / Rebuild a GameCube disc | ✅ |
| `--wiirebuild` | Wiiディスクを再構築 / Rebuild a Wii disc | ✅ |
| `--iso2wbfs` | `.iso` を `.wbfs` に変換 / Convert `.iso` to `.wbfs` | ✅ |
| `--obj2grid` | `.obj` を `grid.bin` + `mapcode.bin` に変換 / Convert `.obj` collision mesh | ✅ |
| `--bmgextract` | `.bmg` をJSONテキストに展開 / Extract `.bmg` to JSON text | ✅ |
| `--bmgpack` | JSONテキストを `.bmg` にパック / Pack JSON text into `.bmg` | ✅ |
| `--bmd2dae` | `.bmd` / `.bdl` をCollada形式 `.dae` に変換 / Convert to Collada `.dae` | ✅ |
| `--bmd2fbx` | `.bmd` / `.bdl` を ASCII `.fbx` + `.glb` に変換 / Convert to ASCII `.fbx` + `.glb` | ✅ |
| `--bmd2obj` | `.bmd` / `.bdl` を `.obj` + `.mtl` に変換 / Convert to `.obj` + `.mtl` | ✅ |
| `--dae2bmd` | Collada形式 `.dae` を `.bmd` に変換 / Convert `.dae` back to `.bmd` | ✅ |
| `--fbx2bmd` | `.fbx` を `.bmd` に変換 / Convert `.fbx` to `.bmd` | ✅ |

## Setup

以下のファイルを同一フォルダにまとめて配置してください．  
`Hocotate_Toolkit` は自己完結型のシングルファイルバイナリのため，.NET のインストールは不要です．

```
Hocotate_Toolkit
resource/
    DiscExtract             <- --gcextract / --wiiextract で使用
    DiscRebuild             <- --gcrebuild / --wiirebuild / --iso2wbfs で使用
    simpleshading.json      <- --fbx2bmd で使用
    BMD_analysis/
        BMD_analysis        <- --bmd2dae / --dae2bmd / --fbx2bmd / --bmd2obj で使用
        libassimp.so        <- BMD_analysis 用ネイティブライブラリ (Assimp 4.1)
        libminizip.so.1     <- libassimp.so の依存ライブラリ (zlibライセンス)
    FBX_analysis/
        FBX_analysis        <- --bmd2fbx で使用
        libassimp.so        <- FBX_analysis 用ネイティブライブラリ
```

> **Note:** `BMD_analysis` と `FBX_analysis` は要求する `libassimp.so` のバージョンが異なるため，
> それぞれ専用のサブフォルダに分けて配置します．フォルダ構成は変更しないでください．  
> `BMD_analysis` and `FBX_analysis` require different versions of `libassimp.so`,
> so each lives in its own subfolder. Do not change this layout.

zip経由で展開した場合は実行権限が失われるため，最初に一度だけ実行権限を付与してください．  
（`resource/` 内のツールは本体が起動時に自動で実行権限を付与します）

If you extracted from a zip, grant execute permission once first.
(Tools inside `resource/` get execute permission automatically at launch.)

```sh
chmod +x Hocotate_Toolkit
```

## Usage

### Command Line / コマンドライン

```
./Hocotate_Toolkit --<mode> <input> [output]
```

出力パスを省略した場合，変換元ファイルと同じ階層に同名のサブフォルダが自動生成され，そこに結果が書き出されます．  
When an output path is omitted, the tool creates a subfolder named after the input file and writes results there.

```sh
# フォルダを .arc にパック / Pack a folder into .arc
./Hocotate_Toolkit --pack "/path/to/folder"

# フォルダを .szs (Yaz0) にパック / Pack a folder into .szs (Yaz0)
./Hocotate_Toolkit --szs "/path/to/folder"

# .arc / .szs を展開 / Extract .arc or .szs
./Hocotate_Toolkit --extract "/path/to/file.szs"

# GameCube ISO/GCM を files + sys に展開 / Extract GameCube ISO/GCM
./Hocotate_Toolkit --gcextract "/path/to/game.iso"

# Wii ISO/WBFS を展開 / Extract Wii ISO/WBFS
./Hocotate_Toolkit --wiiextract "/path/to/game.wbfs"

# sys + files から GameCube ISO を再構築 / Rebuild GameCube ISO
./Hocotate_Toolkit --gcrebuild "/path/to/game_folder"

# sys + files + metadata から Wii ISO/WBFS を再構築 / Rebuild Wii ISO/WBFS
./Hocotate_Toolkit --wiirebuild "/path/to/wii_game_folder"

# ISO を WBFS に変換 / Convert ISO to WBFS
./Hocotate_Toolkit --iso2wbfs "/path/to/game.iso"

# BMD を Collada / FBX / OBJ に変換 / Convert BMD to Collada / FBX / OBJ
./Hocotate_Toolkit --bmd2dae "/path/to/model.bmd"
./Hocotate_Toolkit --bmd2fbx "/path/to/model.bmd"
./Hocotate_Toolkit --bmd2obj "/path/to/model.bmd"

# Collada を BMD に変換 / Convert Collada to BMD
./Hocotate_Toolkit --dae2bmd "/path/to/model.dae" "/path/to/out.bmd" --mat "/path/to/model_materials.json" --texheader "/path/to/model_tex_headers.json"

# FBX を BMD に変換 / Convert FBX to BMD
./Hocotate_Toolkit --fbx2bmd "/path/to/model.fbx"

# OBJ コリジョンメッシュを grid.bin に変換 / Convert OBJ collision mesh
./Hocotate_Toolkit --obj2grid "/path/to/collision.obj"

# BMG を JSON テキストに展開 / Extract BMG to JSON text
./Hocotate_Toolkit --bmgextract "/path/to/message.bmg"

# JSON テキストを BMG にパック / Pack JSON text to BMG
./Hocotate_Toolkit --bmgpack "/path/to/message.txt"

# 文字コードを指定して BMG にパック / Pack BMG with an explicit encoding
./Hocotate_Toolkit --bmgpack "/path/to/message.txt" "/path/to/message.bmg" --encoding shift-jis
```

### Auto Detect / 自動判別

ファイルまたはフォルダを引数1つだけで渡すと，種類を自動判別して適切な変換を実行します（Windows版のドラッグ＆ドロップ相当）．

Pass a single file or folder argument and the tool auto-detects the input type
(equivalent to drag & drop on the Windows build).

```sh
./Hocotate_Toolkit "/path/to/file.szs"
```

| 入力 / Input | 実行される処理 / Action |
|---|---|
| フォルダ / Folder | `--szs` / `--gcrebuild` / `--wiirebuild`（`sys` + `files` があればディスク再構築） |
| `.arc` / `.szs` | `--extract` |
| `.iso` / `.gcm` / `.wbfs` | ディスク全体抽出（GC/Wii自動判別） |
| `.bmd` / `.bdl` | bmd2dae + bmd2fbx + bmd2obj（3種一括 / all three） |
| `.dae` | `--dae2bmd` |
| `.fbx` | `--fbx2bmd` |
| `.obj` | `--obj2grid` |
| `.bmg` | `--bmgextract` |
| `.txt` / `.json` | `--bmgpack` |

> **fbx2bmd の注意 / Note on fbx2bmd:**  
> FBX が参照するテクスチャ画像（PNG等）は FBX と同じフォルダに置いてください．
> `--bmd2dae` の出力フォルダにはテクスチャも展開されるため，bmd2fbx → fbx2bmd の
> 往復変換はそのまま動作します（Windows版と同じ仕様です）．  
> Texture images referenced by the FBX must be placed next to the FBX file.
> The `--bmd2dae` output folder includes extracted textures, so a bmd2fbx → fbx2bmd
> round trip works as-is (same behavior as the Windows build).

### Context Menu / 右クリックメニュー連携

`--register` を実行すると，主要ファイルマネージャの右クリックメニューに用途別の項目が追加されます．  
すべてユーザー単位の設定ファイルへの登録のため，**root権限は不要**です．

Run `--register` to add purpose-specific entries to the right-click menu of major file managers.
Everything is registered as per-user settings — **no root privileges required**.

```sh
# 右クリックメニューに追加 / Add context menu entries
./Hocotate_Toolkit --register

# 右クリックメニューから削除 / Remove context menu entries
./Hocotate_Toolkit --unregister
```

| ファイルマネージャ / File manager | デスクトップ環境 / Desktop | 登録先 / Registered to |
|---|---|---|
| Nautilus | GNOME (Ubuntu等) | `~/.local/share/nautilus/scripts/` |
| Dolphin | KDE | `~/.local/share/kio/servicemenus/` |
| Nemo | Cinnamon (Linux Mint等) | `~/.local/share/nemo/actions/` |
| Thunar | XFCE | `~/.config/Thunar/uca.xml` |

`.arc` / `.szs` / `.gcm` / `.wbfs` / `.bmg` / `.bmd` / `.bdl` / `.fbx` の独自拡張子は，MIMEタイプ定義
（`~/.local/share/mime/packages/`）も同時に登録され，種類別のメニュー出し分けに使われます．

> **Nautilus（GNOME）について:**  
> Nautilusのスクリプト方式はファイル種別での出し分けに対応していないため，
> 「Hocotate Toolkit」（自動判別）と「Convert ISO to WBFS」の2項目が
> 右クリック → スクリプト サブメニューに登録されます．処理結果はデスクトップ通知で表示されます．
>
> **Nautilus (GNOME):** The script mechanism cannot filter by file type, so two entries are
> registered under right-click -> Scripts: "Hocotate Toolkit" (auto detect) and
> "Convert ISO to WBFS". Results are shown as a desktop notification.

> **注意 / Note:** メニューが表示されない場合はファイルマネージャを再起動してください  
> （例: `nautilus -q` / ログインし直し）．  
> If entries do not appear, restart the file manager (e.g. `nautilus -q`) or re-login.

## Build / ビルド方法

.NET SDK 8 以降が必要です．Windows上からのクロスビルドも可能です．

```sh
# Linux上でビルド / Build on Linux
sh publish-release.sh

# Windows上からクロスビルド / Cross-build from Windows
pwsh ./publish-release.ps1
```

出力は `publish/linux-x64/` に配布可能なレイアウトで生成されます．

## Deletion Method / 削除方法

・`Hocotate_Toolkit` が入ったフォルダごと削除してください．  
　右クリックメニューを登録している場合は，先に `./Hocotate_Toolkit --unregister` を実行して登録を解除してください．

・Please delete the entire folder containing `Hocotate_Toolkit`.  
　If you registered the context menu, run `./Hocotate_Toolkit --unregister` first.

## Disclaimer / 免責事項

・本ソフトウェアの使用によって生じたいかなる損害についても，作者は一切の責任を負いません．  
・I assume no responsibility whatsoever for any damages incurred through the use of this software.
