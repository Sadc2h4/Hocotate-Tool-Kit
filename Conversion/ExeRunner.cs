using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace RARCToolkit.Conversion
{
    /// <summary>
    /// 外部 exe をウィンドウなしで実行するユーティリティ。
    /// </summary>
    public static class ExeRunner
    {
        /// <summary>
        /// exe を探す。検索順：
        ///   1. RARCToolkit.exe と同じフォルダの resource\ サブフォルダ
        ///   2. RARCToolkit.exe と同じフォルダ直下
        /// 見つからない場合は <see cref="FileNotFoundException"/> を投げる。
        /// </summary>
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
                $"RARCToolkit.exe と同じ階層の resource\\ フォルダに配置してください。\n" +
                $"例: resource\\{exeName}\n" +
                $"検索元: {baseDir}");
        }

        /// <summary>
        /// 外部 exe を実行し、stdout/stderr を呼び出し元のコンソールに転送する。
        /// </summary>
        /// <param name="exePath">実行ファイルのフルパス</param>
        /// <param name="args">引数リスト（スペースを含む値は自動クォート）</param>
        /// <param name="workingDir">作業ディレクトリ（null の場合は exe と同じディレクトリ）</param>
        /// <param name="captureStderr">非 null の場合、stderr の内容をここに追記する</param>
        /// <returns>プロセスの終了コード</returns>
        public static int Run(string exePath, IEnumerable<string> args, string? workingDir = null,
                              System.Text.StringBuilder? captureStderr = null)
        {
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

        private static string BuildArgString(IEnumerable<string> args)
        {
            return string.Join(" ", args.Select(a =>
                a.Contains(' ') || a.Contains('"') || a.Contains('\t')
                    ? $"\"{a.Replace("\"", "\\\"")}\""
                    : a));
        }

        private static IEnumerable<string> EnumerateCandidates(string baseDir, string exeName)
        {
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

            foreach (string directory in EnumerateBaseDirectories(baseDir))
            {
                string resourceCandidate = Path.Combine(directory, "resource", exeName);
                if (seen.Add(resourceCandidate))
                    yield return resourceCandidate;

                string directCandidate = Path.Combine(directory, exeName);
                if (seen.Add(directCandidate))
                    yield return directCandidate;
            }
        }

        private static IEnumerable<string> EnumerateBaseDirectories(string baseDir)
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
