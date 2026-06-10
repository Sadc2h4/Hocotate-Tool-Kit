using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace RARCToolkit
{
    /// <summary>
    /// Linux用 右クリックメニュー登録ユーティリティ。
    /// Windows版のレジストリ登録（HKCU）に相当する処理を、
    /// 各ファイルマネージャのユーザー設定ファイルへの書き出しで実現する。
    ///   - Nautilus (GNOME)  : ~/.local/share/nautilus/scripts/
    ///   - Dolphin  (KDE)    : ~/.local/share/kio/servicemenus/
    ///   - Nemo     (Cinnamon): ~/.local/share/nemo/actions/
    ///   - Thunar   (XFCE)   : ~/.config/Thunar/uca.xml
    /// 独自拡張子(.arc/.szs等)はMIMEタイプ定義を併せて登録する。
    /// すべてユーザー単位の設定のため管理者権限は不要。
    /// </summary>
    public static class LinuxContextMenu
    {
        // 登録ファイル名に使う共通プレフィックス（アンインストール時の検索キーも兼ねる）
        const string Prefix = "hocotate-toolkit";

        //-------------------------------------------------------------------------------
        // メニュー項目の定義
        //   Id          : 登録ファイル名やキーに使う識別子
        //   Label       : メニューに表示される名前
        //   ModeArg     : 実行時のモード引数（空文字は自動判別モード）
        //   Extensions  : 対象拡張子（ドットなし）
        //   MimeTypes   : Dolphin用のMIMEタイプ
        //   IsDirectory : フォルダ右クリック用の項目か
        //-------------------------------------------------------------------------------
        record MenuEntry(
            string Id,
            string Label,
            string ModeArg,
            string[] Extensions,
            string[] MimeTypes,
            bool IsDirectory = false);

        // Windows版 --register と同等の構成（フェーズ1ではモデル変換系を除く）
        static readonly MenuEntry[] Entries =
        {
            new("extract",     "Hocotate Toolkit - Extract",              "",
                new[] { "arc", "szs" },
                new[] { "application/x-hocotate-arc", "application/x-hocotate-szs" }),

            new("discextract", "Hocotate Toolkit - Extract Disc",         "--discextract",
                new[] { "iso", "gcm" },
                new[] { "application/x-cd-image", "application/x-raw-disk-image", "application/x-hocotate-gcm" }),

            new("iso2wbfs",    "Hocotate Toolkit - Convert ISO to WBFS",  "--iso2wbfs",
                new[] { "iso" },
                new[] { "application/x-cd-image", "application/x-raw-disk-image" }),

            new("wiiextract",  "Hocotate Toolkit - Extract Wii Disc",     "--wiiextract",
                new[] { "wbfs" },
                new[] { "application/x-hocotate-wbfs" }),

            new("bmgextract",  "Hocotate Toolkit - Extract BMG",          "--bmgextract",
                new[] { "bmg" },
                new[] { "application/x-hocotate-bmg" }),

            new("bmgpack",     "Hocotate Toolkit - Pack BMG",             "--bmgpack",
                new[] { "txt", "json" },
                new[] { "text/plain", "application/json" }),

            new("bmdconvert",  "Hocotate Toolkit - Convert BMD",          "",
                new[] { "bmd", "bdl" },
                new[] { "application/x-hocotate-bmd", "application/x-hocotate-bdl" }),

            new("dae2bmd",     "Hocotate Toolkit - DAE to BMD",           "",
                new[] { "dae" },
                new[] { "model/vnd.collada+xml" }),

            new("fbx2bmd",     "Hocotate Toolkit - FBX to BMD",           "",
                new[] { "fbx" },
                new[] { "application/x-hocotate-fbx" }),

            new("obj2grid",    "Hocotate Toolkit - OBJ to grid.bin",      "",
                new[] { "obj" },
                new[] { "model/obj" }),

            new("szspack",     "Hocotate Toolkit - Pack to SZS",          "--szs",
                Array.Empty<string>(),
                new[] { "inode/directory" }, IsDirectory: true),

            new("gcrebuild",   "Hocotate Toolkit - Rebuild GC Disc",      "--gcrebuild",
                Array.Empty<string>(),
                new[] { "inode/directory" }, IsDirectory: true),

            new("wiirebuild",  "Hocotate Toolkit - Rebuild Wii Disc",     "--wiirebuild",
                Array.Empty<string>(),
                new[] { "inode/directory" }, IsDirectory: true),
        };

        // XDG Base Directory（環境変数があればそちらを優先）
        static string DataHome =>
            Environment.GetEnvironmentVariable("XDG_DATA_HOME") is { Length: > 0 } d
                ? d
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");

        static string ConfigHome =>
            Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") is { Length: > 0 } c
                ? c
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");

        //-------------------------------------------------------------------------------
        // 右クリックメニューを登録する処理（Windows版 DoRegister 相当）
        // インストール済みのファイルマネージャを検出し、対応する定義ファイルを書き出す
        //-------------------------------------------------------------------------------
        public static int Register()
        {
            if (OperatingSystem.IsWindows())
            {
                Console.Error.WriteLine("This Linux build's --register only works on Linux.");
                return 1;
            }

            string exePath = Environment.ProcessPath
                          ?? Process.GetCurrentProcess().MainModule!.FileName!;

            try
            {
                // 再登録時に古い定義が残らないよう一度すべて削除する
                RemoveAllEntries(silent: true);

                RegisterMimeTypes();

                bool any = false;
                any |= TryRegister("Nautilus (GNOME)", "nautilus", () => RegisterNautilusScripts(exePath));
                any |= TryRegister("Dolphin (KDE)",    "dolphin",  () => RegisterDolphinServiceMenus(exePath));
                any |= TryRegister("Nemo (Cinnamon)",  "nemo",     () => RegisterNemoActions(exePath));
                any |= TryRegister("Thunar (XFCE)",    "thunar",   () => RegisterThunarActions(exePath));

                if (!any)
                {
                    // 検出できない環境（Dockerなど）でも手動確認できるよう全形式を書き出す
                    Console.WriteLine("No supported file manager detected. Writing entries for all of them.");
                    RegisterNautilusScripts(exePath);
                    RegisterDolphinServiceMenus(exePath);
                    RegisterNemoActions(exePath);
                    RegisterThunarActions(exePath);
                }

                Console.WriteLine();
                Console.WriteLine("Context menu registered successfully.");
                Console.WriteLine("Note: you may need to restart the file manager (e.g. 'nautilus -q') to see the entries.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Registration failed: {ex.Message}");
                return 1;
            }
        }

        //-------------------------------------------------------------------------------
        // 右クリックメニューを削除する処理（Windows版 DoUnregister 相当）
        //-------------------------------------------------------------------------------
        public static int Unregister()
        {
            if (OperatingSystem.IsWindows())
            {
                Console.Error.WriteLine("This Linux build's --unregister only works on Linux.");
                return 1;
            }

            try
            {
                int removed = RemoveAllEntries(silent: false);
                Console.WriteLine($"Context menu unregistered successfully. Removed entries: {removed}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unregistration failed: {ex.Message}");
                return 1;
            }
        }

        //-------------------------------------------------------------------------------
        // ファイルマネージャが存在する場合のみ登録処理を実行する処理
        //-------------------------------------------------------------------------------
        static bool TryRegister(string displayName, string command, Action register)
        {
            if (!CommandExists(command))
            {
                Console.WriteLine($"[SKIP] {displayName}: not installed");
                return false;
            }

            register();
            Console.WriteLine($"[ OK ] {displayName}");
            return true;
        }

        //-------------------------------------------------------------------------------
        // コマンドがPATH上に存在するかを調べる処理（which相当）
        //-------------------------------------------------------------------------------
        static bool CommandExists(string command)
        {
            string? pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathEnv)) return false;

            return pathEnv.Split(Path.PathSeparator)
                          .Where(p => p.Length > 0)
                          .Any(p => File.Exists(Path.Combine(p, command)));
        }

        // ── MIMEタイプ登録 ───────────────────────────────────────────────────

        static string MimePackageFile => Path.Combine(DataHome, "mime", "packages", $"{Prefix}.xml");

        //-------------------------------------------------------------------------------
        // 独自拡張子のMIMEタイプを登録する処理
        // Linuxではメニューの表示条件を拡張子ではなくMIMEタイプで指定するため、
        // .arc/.szs などの未知の拡張子に専用MIMEタイプを対応付ける
        //-------------------------------------------------------------------------------
        static void RegisterMimeTypes()
        {
            XNamespace ns = "http://www.freedesktop.org/standards/shared-mime-info";
            var mimeInfo = new XElement(ns + "mime-info",
                MimeDef(ns, "application/x-hocotate-arc",  "Nintendo RARC archive",          "*.arc"),
                MimeDef(ns, "application/x-hocotate-szs",  "Yaz0-compressed RARC archive",   "*.szs"),
                MimeDef(ns, "application/x-hocotate-gcm",  "Nintendo GameCube disc image",   "*.gcm"),
                MimeDef(ns, "application/x-hocotate-wbfs", "Nintendo Wii disc image (WBFS)", "*.wbfs"),
                MimeDef(ns, "application/x-hocotate-bmg",  "Nintendo BMG message file",      "*.bmg"),
                MimeDef(ns, "application/x-hocotate-bmd",  "Nintendo J3D binary model",      "*.bmd"),
                MimeDef(ns, "application/x-hocotate-bdl",  "Nintendo J3D binary model (BDL)", "*.bdl"),
                MimeDef(ns, "application/x-hocotate-fbx",  "Autodesk FBX model",             "*.fbx"));

            Directory.CreateDirectory(Path.GetDirectoryName(MimePackageFile)!);
            new XDocument(mimeInfo).Save(MimePackageFile);

            UpdateMimeDatabase();
        }

        //-------------------------------------------------------------------------------
        // MIMEタイプ定義1件分のXML要素を作る処理
        //-------------------------------------------------------------------------------
        static XElement MimeDef(XNamespace ns, string type, string comment, string globPattern)
            => new(ns + "mime-type",
                new XAttribute("type", type),
                new XElement(ns + "comment", comment),
                new XElement(ns + "glob", new XAttribute("pattern", globPattern)));

        //-------------------------------------------------------------------------------
        // MIMEデータベースを更新する処理（update-mime-databaseが無い環境では何もしない）
        //-------------------------------------------------------------------------------
        static void UpdateMimeDatabase()
        {
            if (!CommandExists("update-mime-database"))
                return;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "update-mime-database",
                    Arguments = $"\"{Path.Combine(DataHome, "mime")}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit();
            }
            catch
            {
                // 更新失敗は致命的でないため無視（次回ログイン時などに反映される）
            }
        }

        // ── Nautilus (GNOME) ────────────────────────────────────────────────

        static string NautilusScriptsDir => Path.Combine(DataHome, "nautilus", "scripts");

        //-------------------------------------------------------------------------------
        // Nautilus用スクリプトを登録する処理
        // Nautilusのスクリプト方式はファイル種別での出し分けができないため、
        // 自動判別（ドラッグ＆ドロップと同じロジック）に任せる項目を基本とし、
        // 自動判別で到達できない iso2wbfs のみ専用項目を追加する
        //-------------------------------------------------------------------------------
        static void RegisterNautilusScripts(string exePath)
        {
            Directory.CreateDirectory(NautilusScriptsDir);

            WriteNautilusScript("Hocotate Toolkit", exePath, "");
            WriteNautilusScript("Hocotate Toolkit - Convert ISO to WBFS", exePath, "--iso2wbfs");
        }

        //-------------------------------------------------------------------------------
        // Nautilus用スクリプト1件を書き出す処理（実行権限も付与する）
        //-------------------------------------------------------------------------------
        static void WriteNautilusScript(string name, string exePath, string modeArg)
        {
            var sb = new StringBuilder();
            sb.Append("#!/bin/sh\n");
            sb.Append($"# {Prefix} : generated by Hocotate Toolkit --register\n");
            sb.Append("RESULT=0\n");
            sb.Append("IFS='\n'\n");
            sb.Append("for f in $NAUTILUS_SCRIPT_SELECTED_FILE_PATHS; do\n");
            sb.Append(modeArg.Length == 0
                ? $"    \"{exePath}\" \"$f\" < /dev/null > /dev/null 2>&1 || RESULT=1\n"
                : $"    \"{exePath}\" {modeArg} \"$f\" < /dev/null > /dev/null 2>&1 || RESULT=1\n");
            sb.Append("done\n");
            // 完了をデスクトップ通知で知らせる（notify-sendが無い環境では何も出ない）
            sb.Append("if command -v notify-send > /dev/null 2>&1; then\n");
            sb.Append("    if [ $RESULT -eq 0 ]; then\n");
            sb.Append($"        notify-send \"Hocotate Toolkit\" \"Done.\"\n");
            sb.Append("    else\n");
            sb.Append($"        notify-send \"Hocotate Toolkit\" \"Completed with errors.\"\n");
            sb.Append("    fi\n");
            sb.Append("fi\n");
            sb.Append("exit $RESULT\n");

            string path = Path.Combine(NautilusScriptsDir, name);
            File.WriteAllText(path, sb.ToString());
            MakeExecutable(path);
        }

        // ── Dolphin (KDE) ───────────────────────────────────────────────────

        static string DolphinServiceMenuDir => Path.Combine(DataHome, "kio", "servicemenus");

        //-------------------------------------------------------------------------------
        // Dolphin用サービスメニュー(.desktop)を登録する処理
        // 対象MIMEタイプごとに項目を出し分けられるため、Windows版と同じ構成にする
        //-------------------------------------------------------------------------------
        static void RegisterDolphinServiceMenus(string exePath)
        {
            Directory.CreateDirectory(DolphinServiceMenuDir);

            // MIMEタイプの組み合わせが同じ項目は1つの.desktopにまとめる
            foreach (var group in Entries.GroupBy(e => string.Join(";", e.MimeTypes)))
            {
                MenuEntry[] entries = group.ToArray();
                string id = entries[0].Id;

                var sb = new StringBuilder();
                sb.Append("[Desktop Entry]\n");
                sb.Append("Type=Service\n");
                sb.Append($"MimeType={group.Key};\n");
                sb.Append($"Actions={string.Join(";", entries.Select(e => e.Id))};\n");
                sb.Append("X-KDE-Priority=TopLevel\n");

                foreach (MenuEntry entry in entries)
                {
                    sb.Append('\n');
                    sb.Append($"[Desktop Action {entry.Id}]\n");
                    sb.Append($"Name={entry.Label}\n");
                    sb.Append("Icon=package-x-generic\n");
                    sb.Append(entry.ModeArg.Length == 0
                        ? $"Exec=\"{exePath}\" %f\n"
                        : $"Exec=\"{exePath}\" {entry.ModeArg} %f\n");
                }

                string path = Path.Combine(DolphinServiceMenuDir, $"{Prefix}-{id}.desktop");
                File.WriteAllText(path, sb.ToString());
                // KDE Frameworks 5.85以降はサービスメニューに実行権限が必要
                MakeExecutable(path);
            }
        }

        // ── Nemo (Cinnamon) ─────────────────────────────────────────────────

        static string NemoActionsDir => Path.Combine(DataHome, "nemo", "actions");

        //-------------------------------------------------------------------------------
        // Nemo用アクションファイル(.nemo_action)を登録する処理
        // 拡張子フィルタを直接指定できるため、Windows版と同じ構成にする
        //-------------------------------------------------------------------------------
        static void RegisterNemoActions(string exePath)
        {
            Directory.CreateDirectory(NemoActionsDir);

            foreach (MenuEntry entry in Entries)
            {
                var sb = new StringBuilder();
                sb.Append("[Nemo Action]\n");
                sb.Append($"Name={entry.Label}\n");
                sb.Append($"Comment={entry.Label}\n");
                sb.Append(entry.ModeArg.Length == 0
                    ? $"Exec=\"{exePath}\" %F\n"
                    : $"Exec=\"{exePath}\" {entry.ModeArg} %F\n");
                sb.Append("Selection=s\n");
                sb.Append(entry.IsDirectory
                    ? "Extensions=dir;\n"
                    : $"Extensions={string.Join(";", entry.Extensions)};\n");

                string path = Path.Combine(NemoActionsDir, $"{Prefix}-{entry.Id}.nemo_action");
                File.WriteAllText(path, sb.ToString());
            }
        }

        // ── Thunar (XFCE) ───────────────────────────────────────────────────

        static string ThunarUcaFile => Path.Combine(ConfigHome, "Thunar", "uca.xml");

        //-------------------------------------------------------------------------------
        // Thunar用カスタムアクションを登録する処理
        // uca.xmlは他のアクションと共有のファイルのため、
        // 既存内容を保持したままHocotate Toolkitの項目だけを追記する
        //-------------------------------------------------------------------------------
        static void RegisterThunarActions(string exePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ThunarUcaFile)!);

            XDocument doc = File.Exists(ThunarUcaFile)
                ? XDocument.Load(ThunarUcaFile)
                : new XDocument(new XElement("actions"));

            XElement root = doc.Root ?? new XElement("actions");
            if (doc.Root is null) doc.Add(root);

            RemoveThunarActions(root);

            foreach (MenuEntry entry in Entries)
            {
                var action = new XElement("action",
                    new XElement("icon", "package-x-generic"),
                    new XElement("name", entry.Label),
                    // unique-idはThunarが項目を識別するためのキー（プレフィックスで自前項目と判別する）
                    new XElement("unique-id", $"{Prefix}-{entry.Id}"),
                    new XElement("command", entry.ModeArg.Length == 0
                        ? $"\"{exePath}\" %f"
                        : $"\"{exePath}\" {entry.ModeArg} %f"),
                    new XElement("description", entry.Label),
                    new XElement("patterns", entry.IsDirectory
                        ? "*"
                        : string.Join(";", entry.Extensions.Select(e => $"*.{e}"))));

                if (entry.IsDirectory)
                    action.Add(new XElement("directories"));
                else
                    action.Add(new XElement("other-files"));

                root.Add(action);
            }

            doc.Save(ThunarUcaFile);
        }

        //-------------------------------------------------------------------------------
        // uca.xmlからHocotate Toolkitの項目だけを取り除く処理
        // 戻り値: 削除した項目数
        //-------------------------------------------------------------------------------
        static int RemoveThunarActions(XElement root)
        {
            var targets = root.Elements("action")
                .Where(a => (a.Element("unique-id")?.Value ?? "").StartsWith(Prefix, StringComparison.Ordinal)
                         || (a.Element("name")?.Value ?? "").StartsWith("Hocotate Toolkit", StringComparison.Ordinal))
                .ToList();

            foreach (XElement a in targets)
                a.Remove();

            return targets.Count;
        }

        // ── 削除処理 ─────────────────────────────────────────────────────────

        //-------------------------------------------------------------------------------
        // すべてのファイルマネージャから登録済み項目を削除する処理
        // 戻り値: 削除した項目数
        //-------------------------------------------------------------------------------
        static int RemoveAllEntries(bool silent)
        {
            int removed = 0;

            // Nautilus: 「Hocotate Toolkit」で始まる名前のスクリプトを削除
            removed += DeleteFiles(NautilusScriptsDir, "Hocotate Toolkit*", silent);

            // Dolphin / Nemo: プレフィックス付きファイルを削除
            removed += DeleteFiles(DolphinServiceMenuDir, $"{Prefix}-*.desktop", silent);
            removed += DeleteFiles(NemoActionsDir, $"{Prefix}-*.nemo_action", silent);

            // Thunar: uca.xmlから該当項目のみ削除
            if (File.Exists(ThunarUcaFile))
            {
                try
                {
                    XDocument doc = XDocument.Load(ThunarUcaFile);
                    if (doc.Root is not null)
                    {
                        int count = RemoveThunarActions(doc.Root);
                        if (count > 0)
                        {
                            doc.Save(ThunarUcaFile);
                            removed += count;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!silent)
                        Console.Error.WriteLine($"Warning: could not update {ThunarUcaFile}: {ex.Message}");
                }
            }

            // MIME定義を削除してデータベースを更新
            if (File.Exists(MimePackageFile))
            {
                File.Delete(MimePackageFile);
                UpdateMimeDatabase();
                removed++;
            }

            return removed;
        }

        //-------------------------------------------------------------------------------
        // 指定フォルダからパターンに一致するファイルを削除する処理
        // 戻り値: 削除したファイル数
        //-------------------------------------------------------------------------------
        static int DeleteFiles(string directory, string pattern, bool silent)
        {
            if (!Directory.Exists(directory))
                return 0;

            int removed = 0;
            foreach (string file in Directory.GetFiles(directory, pattern))
            {
                try
                {
                    File.Delete(file);
                    removed++;
                }
                catch (Exception ex)
                {
                    if (!silent)
                        Console.Error.WriteLine($"Warning: could not delete {file}: {ex.Message}");
                }
            }
            return removed;
        }

        //-------------------------------------------------------------------------------
        // ファイルに実行権限を付与する処理（chmod +x相当）
        //-------------------------------------------------------------------------------
        static void MakeExecutable(string path)
        {
            if (OperatingSystem.IsWindows())
                return;

            try
            {
                File.SetUnixFileMode(path,
                    File.GetUnixFileMode(path) |
                    UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
            }
            catch
            {
                // 権限付与に失敗してもファイル自体は配置済みのため続行
            }
        }
    }
}
