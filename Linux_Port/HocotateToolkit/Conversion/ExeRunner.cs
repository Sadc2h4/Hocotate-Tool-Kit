using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace RARCToolkit.Conversion
{
    /// <summary>
    /// 外部実行ファイルをウィンドウなしで実行するユーティリティ（Linux版）。
    /// Windows版との違い:
    ///   - 拡張子なしのバイナリ名（例: DiscExtract）を優先して探す
    ///   - zip展開などで実行権限が失われたバイナリに実行権限を自動付与する
    /// </summary>
    public static class ExeRunner
    {
        //-------------------------------------------------------------------------------
        // 実行ファイルを探す処理
        // 検索順:
        //   1. Hocotate_Toolkit と同じフォルダの resource/<ツール名>/ サブフォルダ
        //      （BMD_analysis と FBX_analysis は異なるバージョンの libassimp.so を
        //        必要とするため、ツールごとの専用フォルダに分離して配置する）
        //   2. Hocotate_Toolkit と同じフォルダの resource/ サブフォルダ直下
        //   3. Hocotate_Toolkit と同じフォルダ直下
        //   （見つかるまで親フォルダ方向へさかのぼる）
        // 見つからない場合は FileNotFoundException を投げる。
        //-------------------------------------------------------------------------------
        public static string FindExe(string exeName)
        {
            string baseDir = AppContext.BaseDirectory;
            foreach (string candidate in EnumerateCandidates(baseDir, exeName))
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            throw new FileNotFoundException(
                $"'{exeName}' が見つかりません。\n" +
                $"Hocotate_Toolkit と同じ階層の resource/ フォルダに配置してください。\n" +
                $"例: resource/{exeName}\n" +
                $"検索元: {baseDir}");
        }

        //-------------------------------------------------------------------------------
        // 外部実行ファイルを起動し、stdout/stderr を呼び出し元のコンソールに転送する処理
        //   exePath       : 実行ファイルのフルパス
        //   args          : 引数リスト（スペースを含む値は自動クォート）
        //   workingDir    : 作業ディレクトリ（null の場合はカレントディレクトリ）
        //   captureStderr : 非 null の場合、stderr の内容をここに追記する
        // 戻り値: プロセスの終了コード
        //-------------------------------------------------------------------------------
        public static int Run(string exePath, IEnumerable<string> args, string? workingDir = null,
                              System.Text.StringBuilder? captureStderr = null)
        {
            EnsureExecutable(exePath);

            string argString = BuildArgString(args);

            var psi = new ProcessStartInfo
            {
                FileName               = exePath,
                Arguments              = argString,
                WorkingDirectory       = workingDir ?? Environment.CurrentDirectory,
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true,
            };

            // 同梱のネイティブライブラリ（libassimp.so の依存 libminizip.so.1 等）を
            // ツールと同じフォルダから解決できるよう LD_LIBRARY_PATH に追加する
            if (!OperatingSystem.IsWindows())
            {
                string exeDir = Path.GetDirectoryName(exePath) ?? ".";
                string current = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH") ?? "";
                psi.Environment["LD_LIBRARY_PATH"] = string.IsNullOrEmpty(current)
                    ? exeDir
                    : $"{exeDir}:{current}";
            }

            using var proc = Process.Start(psi)
                ?? throw new InvalidOperationException($"プロセスを起動できませんでした: {exePath}");

            // 出力を非同期で転送（デッドロック回避）
            proc.OutputDataReceived += (_, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
            proc.ErrorDataReceived  += (_, e) =>
            {
                if (e.Data == null) return;
                Console.Error.WriteLine(e.Data);
                captureStderr?.AppendLine(e.Data);
            };
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            proc.WaitForExit();
            return proc.ExitCode;
        }

        //-------------------------------------------------------------------------------
        // Linux環境で実行権限が無い場合に chmod +x 相当を付与する処理
        // （zip経由の配布では実行権限が落ちるため起動前に必ず確認する）
        //-------------------------------------------------------------------------------
        static void EnsureExecutable(string exePath)
        {
            if (OperatingSystem.IsWindows())
                return;

            try
            {
                UnixFileMode mode = File.GetUnixFileMode(exePath);
                UnixFileMode exec = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
                if ((mode & exec) == 0)
                    File.SetUnixFileMode(exePath, mode | exec);
            }
            catch
            {
                // 権限変更に失敗しても起動は試みる（読み取り専用FSなど）
            }
        }

        static string BuildArgString(IEnumerable<string> args)
        {
            return string.Join(" ", args.Select(a =>
                a.Contains(' ') || a.Contains('"') || a.Contains('\t')
                    ? $"\"{a.Replace("\"", "\\\"")}\""
                    : a));
        }

        //-------------------------------------------------------------------------------
        // 実行ファイルの探索候補パスを列挙する処理
        // Linuxでは拡張子なしの名前を優先し、互換のため .exe 付きの名前も候補に含める
        //-------------------------------------------------------------------------------
        static IEnumerable<string> EnumerateCandidates(string baseDir, string exeName)
        {
            // Linuxバイナリは拡張子なしが標準のため両方の名前を試す
            string[] names = exeName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? new[] { exeName[..^4], exeName }
                : new[] { exeName };

            HashSet<string> seen = new(StringComparer.Ordinal);

            foreach (string directory in EnumerateBaseDirectories(baseDir))
            {
                foreach (string name in names)
                {
                    // ツール専用サブフォルダ（resource/BMD_analysis/BMD_analysis 等）を最優先で探す
                    string toolDirCandidate = Path.Combine(directory, "resource", name, name);
                    if (seen.Add(toolDirCandidate))
                        yield return toolDirCandidate;

                    string resourceCandidate = Path.Combine(directory, "resource", name);
                    if (seen.Add(resourceCandidate))
                        yield return resourceCandidate;

                    string directCandidate = Path.Combine(directory, name);
                    if (seen.Add(directCandidate))
                        yield return directCandidate;
                }
            }
        }

        static IEnumerable<string> EnumerateBaseDirectories(string baseDir)
        {
            DirectoryInfo? current = new(baseDir);
            while (current is not null)
            {
                yield return current.FullName;
                current = current.Parent;
            }
        }
    }
}
