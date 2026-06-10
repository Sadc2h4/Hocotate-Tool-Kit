using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RARCToolkit.Collision;
using RARCToolkit.Compression;
using RARCToolkit.Conversion;
using RARCToolkit.IO;
using RARCToolkit.RARC;

namespace RARCToolkit
{
    /// <summary>
    /// Hocotate Toolkit Linux版 エントリポイント。
    /// Windows版との違い:
    ///   - 右クリックメニュー登録はレジストリではなく各ファイルマネージャの設定ファイルで行う
    ///   - 外部ツール（BMD_analysis / FBX_analysis / DiscExtract / DiscRebuild）は
    ///     Linux向けにビルドした拡張子なしバイナリを resource/ から呼び出す
    /// </summary>
    class Program
    {
        // ── ASCII art banner ─────────────────────────────────────────────────
        static readonly string[] Banner =
        {
            @"                       _        _         _____            _   _    _ _   ",
            @"  /\  /\___   ___ ___ | |_ __ _| |_ ___  /__   \___   ___ | | | | _(_) |_ ",
            @" / /_/ / _ \ / __/ _ \| __/ _` | __/ _ \   / /\/ _ \ / _ \| | | |/ / | __|",
            @"/ __  / (_) | (_| (_) | || (_| | ||  __/  / / | (_) | (_) | | |   <| | |_ ",
            @"\/ /_/ \___/ \___\___/ \__\__,_|\__\___|  \/   \___/ \___/|_| |_|\_\_|\__|",
            @"Created by : C2H4",
        };

        static void PrintBanner()
        {
            foreach (var line in Banner) Console.WriteLine(line);
            Console.WriteLine($"Version    : {GetDisplayVersion()} (Linux)");
            Console.WriteLine();
        }

        static string GetDisplayVersion()
            => typeof(Program).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion
               ?? typeof(Program).Assembly.GetName().Version?.ToString()
               ?? "unknown";

        // ── Entry point ──────────────────────────────────────────────────────

        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintBanner();
                PrintUsage();
                Console.WriteLine();
                Console.WriteLine("──────────────────────────────────────────────────────────────");
                Console.WriteLine("  This tool is designed to be used in two ways:");
                Console.WriteLine("  1. Right-click a file in your file manager (after --register).");
                Console.WriteLine("  2. Call from a shell script or terminal with arguments.");
                Console.WriteLine("──────────────────────────────────────────────────────────────");
                Console.WriteLine();
                WaitForKeyIfInteractive();
                return 1;
            }

            // File manager / drag & drop: first arg is a path, not a flag
            if (!args[0].StartsWith("-"))
                return AutoDetect(args[0]);

            string mode = args[0].TrimStart('-').ToLower();

            // register / unregister do not need a second argument
            if (mode == "register")   return LinuxContextMenu.Register();
            if (mode == "unregister") return LinuxContextMenu.Unregister();

            if (args.Length < 2)
            {
                PrintBanner();
                PrintUsage();
                return 1;
            }

            string input = args[1];

            try
            {
                return mode switch
                {
                    "pack"     => DoPack(input, OptArg(args, 2)),
                    "szs"      => DoSzs(input,  OptArg(args, 2)),
                    "extract"  => DoExtract(input, OptArg(args, 2)),
                    "gcextract"=> DoGcExtract(input, OptArg(args, 2)),
                    "wiiextract"=> DoWiiExtract(input, OptArg(args, 2)),
                    "wiextract"=> DoWiiExtract(input, OptArg(args, 2)),
                    "discextract"=> DoDiscExtract(input, OptArg(args, 2)),
                    "gcrebuild"=> DoGcRebuild(input, OptArg(args, 2)),
                    "wiirebuild"=> DoWiiRebuild(input, OptArg(args, 2)),
                    "wirebuild"=> DoWiiRebuild(input, OptArg(args, 2)),
                    "discrebuild"=> DoDiscRebuild(input, OptArg(args, 2)),
                    "iso2wbfs"=> DoIsoToWbfs(input, OptArg(args, 2)),
                    "isotowbfs"=> DoIsoToWbfs(input, OptArg(args, 2)),
                    "bmd2dae"  => DoBmd2Dae(input, OptArg(args, 2)),
                    "dae2bmd"  => DoDae2Bmd(args),
                    "bmd2fbx"  => DoBmd2Fbx(input, OptArg(args, 2)),
                    "fbx2bmd"  => DoFbx2Bmd(input, OptArg(args, 2)),
                    "bmd2obj"  => DoBmd2Obj(input, OptArg(args, 2)),
                    "obj2grid" => DoObj2Grid(args),
                    "bmgextract" => DoBmgExtract(input, OptArg(args, 2)),
                    "bmgpack" => DoBmgPack(args),
                    _ => UnknownMode(args[0]),
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"\nError: {ex.Message}");
                return 1;
            }
        }

        // ── File manager / drag & drop auto-detect ───────────────────────────

        static int AutoDetect(string path)
        {
            path = NormalizePath(path);
            PrintBanner();
            Console.WriteLine($"Input: {path}");
            Console.WriteLine();

            int result;
            try
            {
                if (Directory.Exists(path))
                {
                    if (LooksLikeWiiDiscRoot(path))
                    {
                        Console.WriteLine("[Folder] -> Rebuild Wii disc");
                        result = DoWiiRebuild(path, null);
                    }
                    else if (LooksLikeGameCubeDiscRoot(path))
                    {
                        Console.WriteLine("[Folder] -> Rebuild GameCube disc");
                        result = DoGcRebuild(path, null);
                    }
                    else
                    {
                        Console.WriteLine("[Folder] -> SZS pack");
                        result = DoSzs(path, null);
                    }
                }
                else if (File.Exists(path))
                {
                    string ext = Path.GetExtension(path).ToLower();
                    result = ext switch
                    {
                        ".arc" or ".szs" => RunExtract(path),
                        ".iso" or ".gcm" or ".wbfs" => RunDiscExtract(path),
                        ".bmd" or ".bdl" => RunBmdAll(path),
                        ".dae"           => RunDae2Bmd(path),
                        ".fbx"           => RunFbx2Bmd(path),
                        ".obj"           => RunObj2Grid(path),
                        ".bmg"           => RunBmgExtract(path),
                        ".txt" or ".json"=> RunBmgPack(path),
                        _                => UnknownDrop(ext),
                    };
                }
                else
                {
                    Console.Error.WriteLine($"File or folder not found: {path}");
                    result = 1;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"\nError: {ex.Message}");
                result = 1;
            }

            Console.WriteLine();
            Console.WriteLine("──────────────────────────────────────────────────────────────");
            Console.WriteLine(result == 0 ? "Done." : "Completed with errors.");
            WaitForKeyIfInteractive();
            return result;
        }

        static int RunExtract(string path)
        {
            Console.WriteLine($"[{Path.GetExtension(path).ToUpper()}] -> Extract");
            return DoExtract(path, null);
        }

        static int RunDiscExtract(string path)
        {
            Console.WriteLine("[ISO/GCM/WBFS] -> Extract disc");
            return DoDiscExtract(path, null);
        }

        static int RunBmdAll(string path)
        {
            Console.WriteLine("[BMD/BDL] -> bmd2dae / bmd2fbx / bmd2obj (batch)");
            Console.WriteLine();
            int overall = 0;
            RunStep("bmd2dae", () => DoBmd2Dae(path, null), ref overall);
            RunStep("bmd2fbx", () => DoBmd2Fbx(path, null), ref overall);
            RunStep("bmd2obj", () => DoBmd2Obj(path, null), ref overall);
            return overall;
        }

        static int RunDae2Bmd(string path)
        {
            Console.WriteLine("[DAE] -> BMD conversion");
            return DoDae2Bmd(new[] { "--dae2bmd", path });
        }

        static int RunFbx2Bmd(string path)
        {
            Console.WriteLine("[FBX] -> BMD conversion");
            return DoFbx2Bmd(path, null);
        }

        static void RunStep(string label, Func<int> action, ref int overall)
        {
            Console.WriteLine($"── {label} ──────────────────────");
            try
            {
                int r = action();
                if (r != 0)
                {
                    Console.Error.WriteLine($"  [{label}] Exit code: {r}");
                    overall = r;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  [{label}] Error: {ex.Message}");
                overall = 1;
            }
            Console.WriteLine();
        }

        static int RunObj2Grid(string path)
        {
            Console.WriteLine("[OBJ] -> grid.bin + mapcode.bin");
            return DoObj2Grid(new[] { "--obj2grid", path });
        }

        static int UnknownDrop(string ext)
        {
            Console.Error.WriteLine($"Unsupported file type: {ext}");
            Console.Error.WriteLine("Supported: folder, .arc, .szs, .iso, .gcm, .wbfs, .bmd, .bdl, .dae, .fbx, .obj, .bmg, .txt, .json");
            return 1;
        }

        // ── RARC pack / extract ───────────────────────────────────────────────

        static int DoPack(string folderPath, string? outputPath)
        {
            folderPath = NormalizePath(folderPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            RequireDirectory(folderPath);
            outputPath = NormalizeOutputPath(outputPath) ?? SiblingFile(folderPath, ".arc");
            Console.WriteLine($"Pack: {folderPath} -> {outputPath}");
            PackFolder(folderPath, outputPath);
            Console.WriteLine("Done.");
            return 0;
        }

        static int DoSzs(string folderPath, string? outputPath)
        {
            folderPath = NormalizePath(folderPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            RequireDirectory(folderPath);
            outputPath = NormalizeOutputPath(outputPath) ?? SiblingFile(folderPath, ".szs");
            Console.WriteLine($"SZS pack: {folderPath} -> {outputPath}");
            PackFolderToSzs(folderPath, outputPath);
            Console.WriteLine("Done.");
            return 0;
        }

        static int DoExtract(string inputPath, string? outputDir)
        {
            inputPath = NormalizePath(inputPath);
            RequireFile(inputPath);
            outputDir = NormalizeOutputPath(outputDir) ?? Path.Combine(
                Path.GetDirectoryName(inputPath) ?? ".",
                Path.GetFileNameWithoutExtension(inputPath));
            Console.WriteLine($"Extract: {inputPath} -> {outputDir}");
            new RARCExtractor().Extract(inputPath, outputDir);
            return 0;
        }

        static int DoGcExtract(string inputPath, string? outputDir)
        {
            inputPath = NormalizePath(inputPath);
            RequireFile(inputPath);
            outputDir = NormalizeOutputPath(outputDir);
            Console.WriteLine($"GC extract: {inputPath} -> {outputDir ?? "(auto)"}");
            return DiscExtractConvert.ExtractGameCubeDisc(inputPath, outputDir);
        }

        static int DoWiiExtract(string inputPath, string? outputDir)
        {
            inputPath = NormalizePath(inputPath);
            RequireFile(inputPath);
            outputDir = NormalizeOutputPath(outputDir);
            Console.WriteLine($"Wii extract: {inputPath} -> {outputDir ?? "(auto)"}");
            return DiscExtractConvert.ExtractWiiDisc(inputPath, outputDir);
        }

        static int DoDiscExtract(string inputPath, string? outputDir)
        {
            inputPath = NormalizePath(inputPath);
            RequireFile(inputPath);
            outputDir = NormalizeOutputPath(outputDir);
            Console.WriteLine($"Disc extract: {inputPath} -> {outputDir ?? "(auto)"}");
            return DiscExtractConvert.ExtractDiscImage(inputPath, outputDir);
        }

        static int DoGcRebuild(string inputFolder, string? outputPath)
        {
            inputFolder = NormalizePath(inputFolder);
            RequireDirectory(inputFolder);
            outputPath = NormalizeOutputPath(outputPath);
            Console.WriteLine($"GC rebuild: {inputFolder} -> {outputPath ?? "(auto)"}");
            return DiscRebuildConvert.RebuildGameCubeDisc(inputFolder, outputPath);
        }

        static int DoWiiRebuild(string inputFolder, string? outputPath)
        {
            inputFolder = NormalizePath(inputFolder);
            RequireDirectory(inputFolder);
            outputPath = NormalizeOutputPath(outputPath);
            Console.WriteLine($"Wii rebuild: {inputFolder} -> {outputPath ?? "(auto)"}");
            return DiscRebuildConvert.RebuildWiiDisc(inputFolder, outputPath);
        }

        static int DoIsoToWbfs(string inputIso, string? outputPath)
        {
            inputIso = NormalizePath(inputIso);
            RequireFile(inputIso);
            outputPath = NormalizeOutputPath(outputPath);
            Console.WriteLine($"ISO to WBFS: {inputIso} -> {outputPath ?? "(auto)"}");
            return DiscRebuildConvert.ConvertIsoToWbfs(inputIso, outputPath);
        }

        static int DoDiscRebuild(string inputFolder, string? outputPath)
        {
            inputFolder = NormalizePath(inputFolder);
            RequireDirectory(inputFolder);
            outputPath = NormalizeOutputPath(outputPath);
            Console.WriteLine($"Disc rebuild: {inputFolder} -> {outputPath ?? "(auto)"}");
            return DiscRebuildConvert.RebuildDisc(inputFolder, outputPath);
        }

        // ── BMG text pack / extract ─────────────────────────────────────────

        //-------------------------------------------------------------------------------
        // BMGファイルを右クリック/自動判別で展開する処理
        //-------------------------------------------------------------------------------
        static int RunBmgExtract(string path)
        {
            Console.WriteLine("[BMG] -> JSON text extract");
            return DoBmgExtract(path, null);
        }

        //-------------------------------------------------------------------------------
        // BMG用JSONテキストを右クリック/自動判別でパックする処理
        //-------------------------------------------------------------------------------
        static int RunBmgPack(string path)
        {
            Console.WriteLine("[BMG text] -> BMG pack");
            return DoBmgPack(new[] { "--bmgpack", path });
        }

        //-------------------------------------------------------------------------------
        // BMGファイルをJSONテキストへ展開する処理
        //-------------------------------------------------------------------------------
        static int DoBmgExtract(string inputBmg, string? outputText)
        {
            inputBmg = NormalizePath(inputBmg);
            RequireFile(inputBmg);
            outputText = NormalizeOutputPath(outputText) ?? Path.Combine(
                Path.GetDirectoryName(inputBmg) ?? ".",
                Path.GetFileNameWithoutExtension(inputBmg) + ".txt");
            Console.WriteLine($"BMG extract: {inputBmg} -> {outputText}");
            BmgConvert.Extract(inputBmg, outputText);
            Console.WriteLine("Done.");
            return 0;
        }

        //-------------------------------------------------------------------------------
        // JSONテキストをBMGファイルへパックする処理
        //-------------------------------------------------------------------------------
        static int DoBmgPack(string[] args)
        {
            string inputText = NormalizePath(args[1]);
            RequireFile(inputText);
            string? outputBmg = NormalizeOutputPath(OptArg(args, 2));
            outputBmg ??= Path.Combine(
                Path.GetDirectoryName(inputText) ?? ".",
                Path.GetFileNameWithoutExtension(inputText) + ".bmg");
            string? encoding = GetFlag(args, "--encoding");
            Console.WriteLine($"BMG pack: {inputText} -> {outputBmg}");
            BmgConvert.Pack(inputText, outputBmg, encoding);
            Console.WriteLine("Done.");
            return 0;
        }

        // ── BMD_analysis / FBX_analysis wrapper ──────────────────────────────

        static int DoBmd2Dae(string inputBmd, string? outputDae)
        {
            inputBmd = NormalizePath(inputBmd);
            RequireFile(inputBmd);
            if (string.IsNullOrEmpty(outputDae))
            {
                string dir       = Path.GetDirectoryName(inputBmd) ?? ".";
                string name      = Path.GetFileNameWithoutExtension(inputBmd);
                string outFolder = Path.Combine(dir, name);
                Directory.CreateDirectory(outFolder);
                outputDae        = Path.Combine(outFolder, name + ".dae");
            }
            else outputDae = NormalizeOutputPath(outputDae);
            return SuperBMDConvert.Bmd2Dae(inputBmd, outputDae);
        }

        static int DoDae2Bmd(string[] args)
        {
            string inputDae = NormalizePath(args[1]);
            RequireFile(inputDae);
            string? output = NormalizeOutputPath(OptArg(args, 2));
            string? mat    = NormalizePathOrNull(GetFlag(args, "--mat"));
            string? texhdr = NormalizePathOrNull(GetFlag(args, "--texheader"));
            return SuperBMDConvert.Dae2Bmd(inputDae, output, mat, texhdr);
        }

        static int DoBmd2Fbx(string inputBmd, string? outputDir)
        {
            inputBmd = NormalizePath(inputBmd);
            RequireFile(inputBmd);
            outputDir = NormalizeOutputPath(outputDir);
            return SuperBMDConvert.Bmd2Fbx(inputBmd, outputDir);
        }

        static int DoFbx2Bmd(string inputFbx, string? outputBmd)
        {
            inputFbx = NormalizePath(inputFbx);
            RequireFile(inputFbx);
            outputBmd = NormalizeOutputPath(outputBmd);
            return SuperBMDConvert.Model2Bmd(inputFbx, outputBmd, FindSiblingOrResource(inputFbx, "simpleshading.json"), null);
        }

        static int DoBmd2Obj(string inputBmd, string? outputObj)
        {
            inputBmd = NormalizePath(inputBmd);
            RequireFile(inputBmd);
            if (string.IsNullOrEmpty(outputObj))
            {
                string dir       = Path.GetDirectoryName(inputBmd) ?? ".";
                string name      = Path.GetFileNameWithoutExtension(inputBmd);
                string outFolder = Path.Combine(dir, name);
                Directory.CreateDirectory(outFolder);
                outputObj        = Path.Combine(outFolder, name + ".obj");
            }
            else outputObj = NormalizeOutputPath(outputObj);
            return SuperBMDConvert.Bmd2Obj(inputBmd, outputObj);
        }

        //-------------------------------------------------------------------------------
        // 入力ファイルの隣 → resource/ フォルダの順で対象ファイルを探す処理
        // （fbx2bmd 用の simpleshading.json 検索などに使用）
        //-------------------------------------------------------------------------------
        static string? FindSiblingOrResource(string inputPath, string fileName)
        {
            string? dir = Path.GetDirectoryName(inputPath);
            if (!string.IsNullOrEmpty(dir))
            {
                string sibling = Path.Combine(dir, fileName);
                if (File.Exists(sibling))
                    return sibling;
            }

            DirectoryInfo? current = new(AppContext.BaseDirectory);
            while (current is not null)
            {
                string resource = Path.Combine(current.FullName, "resource", fileName);
                if (File.Exists(resource))
                    return resource;

                string direct = Path.Combine(current.FullName, fileName);
                if (File.Exists(direct))
                    return direct;

                current = current.Parent;
            }

            return null;
        }

        // ── obj2grid ─────────────────────────────────────────────────────────

        static int DoObj2Grid(string[] args)
        {
            string inputObj = NormalizePath(args[1]);
            RequireFile(inputObj);

            string? outputGrid    = NormalizeOutputPath(OptArg(args, 2));
            string? outputMapcode = NormalizeOutputPath(OptArg(args, 3));
            int     cellSize      = int.TryParse(GetFlag(args, "--cell_size"), out int cs) ? cs : 100;
            bool    flipYZ        = args.Contains("--flipyz");

            if (string.IsNullOrEmpty(outputGrid))
            {
                string dir       = Path.GetDirectoryName(inputObj) ?? ".";
                string name      = Path.GetFileNameWithoutExtension(inputObj);
                string outFolder = Path.Combine(dir, name);
                Directory.CreateDirectory(outFolder);
                outputGrid    = Path.Combine(outFolder, "grid.bin");
                outputMapcode ??= Path.Combine(outFolder, "mapcode.bin");
            }

            new Obj2Grid().Convert(inputObj, outputGrid, outputMapcode, cellSize, flipYZ);
            return 0;
        }

        static string NormalizePath(string path)
            => Path.GetFullPath(path);

        static string? NormalizePathOrNull(string? path)
            => string.IsNullOrWhiteSpace(path) ? null : NormalizePath(path);

        static string? NormalizeOutputPath(string? path)
            => string.IsNullOrWhiteSpace(path) ? null : NormalizePath(path);

        static void WaitForKeyIfInteractive()
        {
            try
            {
                if (!Environment.UserInteractive ||
                    Console.IsInputRedirected ||
                    Console.IsOutputRedirected ||
                    Console.IsErrorRedirected)
                    return;

                Console.WriteLine();
                Console.WriteLine("Press any key to close...");
                Console.ReadKey();
            }
            catch
            {
            }
        }

        // ── Common helpers ────────────────────────────────────────────────────

        static void PackFolder(string folderPath, string outputPath)
        {
            VirtualFolder root = BuildVirtualFolder(folderPath, isRoot: true);
            using var fs     = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            using var writer = new EndianBinaryWriter(fs);
            new RARCPacker().Pack(root, writer);
        }

        //-------------------------------------------------------------------------------
        // フォルダをRARCへパックした後にYaz0圧縮してSZSへ保存する処理
        //-------------------------------------------------------------------------------
        static void PackFolderToSzs(string folderPath, string outputPath)
        {
            VirtualFolder root = BuildVirtualFolder(folderPath, isRoot: true);
            using var ms = new MemoryStream();
            using (var writer = new EndianBinaryWriter(ms))
                new RARCPacker().Pack(root, writer);

            File.WriteAllBytes(outputPath, Yaz0.Compress(ms.ToArray()));
        }

        static VirtualFolder BuildVirtualFolder(string dirPath, bool isRoot)
        {
            string name = Path.GetFileName(
                dirPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            string nodeName = isRoot ? "ROOT"
                : name.Length >= 4 ? name[..4].ToUpper()
                : name.ToUpper().PadRight(4);
            var folder = new VirtualFolder { Name = name, NodeName = nodeName };
            foreach (string sub in Directory.GetDirectories(dirPath).OrderBy(x => x))
                folder.Subdirs.Add(BuildVirtualFolder(sub, isRoot: false));
            foreach (string file in Directory.GetFiles(dirPath).OrderBy(x => x))
                folder.Files.Add(new FileData { Name = Path.GetFileName(file), Data = File.ReadAllBytes(file) });
            return folder;
        }

        static string? OptArg(string[] args, int index)
        {
            if (index >= args.Length) return null;
            string v = args[index];
            return v.StartsWith("--") || v.StartsWith("-") ? null : v;
        }

        static string? GetFlag(string[] args, string flag)
        {
            int idx = Array.IndexOf(args, flag);
            return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
        }

        static string SiblingFile(string dirPath, string ext) =>
            Path.Combine(Path.GetDirectoryName(dirPath) ?? ".", Path.GetFileName(dirPath) + ext);

        static bool LooksLikeGameCubeDiscRoot(string path)
        {
            string sys = Path.Combine(path, "sys");
            string files = Path.Combine(path, "files");
            return Directory.Exists(sys) &&
                   Directory.Exists(files) &&
                   File.Exists(Path.Combine(sys, "boot.bin")) &&
                   File.Exists(Path.Combine(sys, "bi2.bin")) &&
                   File.Exists(Path.Combine(sys, "apploader.img")) &&
                   File.Exists(Path.Combine(sys, "main.dol"));
        }

        static bool LooksLikeWiiDiscRoot(string path)
        {
            if (!LooksLikeGameCubeDiscRoot(path))
                return false;

            return Directory.Exists(Path.Combine(path, "disc")) &&
                   File.Exists(Path.Combine(path, "ticket.bin")) &&
                   File.Exists(Path.Combine(path, "tmd.bin")) &&
                   File.Exists(Path.Combine(path, "cert.bin"));
        }

        static void RequireFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"File not found: {path}");
        }

        static void RequireDirectory(string path)
        {
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"Folder not found: {path}");
        }

        static int UnknownMode(string mode)
        {
            Console.Error.WriteLine($"Unknown mode: {mode}");
            PrintUsage();
            return 1;
        }

        static void PrintUsage()
        {
            Console.WriteLine("Credits");
            Console.WriteLine("  Hocotate Toolkit is created by C2H4.");
            Console.WriteLine("  The following applications and reference authors were consulted during implementation.");
            Console.WriteLine();
            Console.WriteLine("  Operation   Reference Application   Reference Author");
            Console.WriteLine("  --pack      RARCPack                Yoshi2");
            Console.WriteLine("  --szs       RARCPack                Yoshi2");
            Console.WriteLine("  --extract   ARCExtractor            cuzitsjonny");
            Console.WriteLine("  --gcextract DiscExtract             jordan-woyak");
            Console.WriteLine("  --wiiextract DiscExtract            jordan-woyak");
            Console.WriteLine("  --gcrebuild DiscRebuild             jordan-woyak");
            Console.WriteLine("  --wiirebuild DiscRebuild            jordan-woyak");
            Console.WriteLine("  --iso2wbfs  DiscRebuild             jordan-woyak");
            Console.WriteLine("  --bmd2dae   BMD_analysis v2    RenolY2");
            Console.WriteLine("  --bmd2fbx   FBX_analysis v2    MeltyPlayer");
            Console.WriteLine("  --bmd2obj   BMD_analysis v2    RenolY2");
            Console.WriteLine("  --dae2bmd   BMD_analysis v2    RenolY2");
            Console.WriteLine("  --fbx2bmd   BMD_analysis v2    RenolY2");
            Console.WriteLine("  --obj2grid  obj2grid           RenolY2");
            Console.WriteLine("  --bmgextract cube / pikminBMG  Yoshi2 / riidefi");
            Console.WriteLine("  --bmgpack    cube / pikminBMG  Yoshi2 / riidefi");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine();
            Console.WriteLine("  [Auto Detect]  Pass a file or folder as the only argument:");
            Console.WriteLine("    Folder           -> Pack to SZS (.szs)");
            Console.WriteLine("                      -> Rebuild GC/Wii disc if sys + files are present");
            Console.WriteLine("    .arc / .szs      -> Extract");
            Console.WriteLine("    .iso / .gcm      -> Extract GameCube or Wii disc");
            Console.WriteLine("    .wbfs            -> Extract Wii disc");
            Console.WriteLine("    .bmd / .bdl      -> Convert to DAE + FBX + OBJ (batch)");
            Console.WriteLine("    .dae             -> Convert to BMD");
            Console.WriteLine("    .fbx             -> Convert to BMD");
            Console.WriteLine("    .obj             -> Generate grid.bin + mapcode.bin");
            Console.WriteLine("    .bmg             -> Extract to JSON text");
            Console.WriteLine("    .txt / .json     -> Pack to BMG");
            Console.WriteLine();
            Console.WriteLine("  [Command Line]");
            Console.WriteLine("    --pack     <folder>       [output.arc]");
            Console.WriteLine("    --szs      <folder>       [output.szs]");
            Console.WriteLine("    --extract  <.arc/.szs>    [output folder]");
            Console.WriteLine("    --gcextract <.iso/.gcm>   [output folder]");
            Console.WriteLine("    --wiiextract <.iso/.wbfs> [output folder]");
            Console.WriteLine("    --discextract <disc image> [output folder]");
            Console.WriteLine("    --gcrebuild <folder>      [output.iso/.gcm]");
            Console.WriteLine("    --wiirebuild <folder>     [output.iso/.wbfs]");
            Console.WriteLine("    --discrebuild <folder>    [output image]");
            Console.WriteLine("    --iso2wbfs  <input.iso>   [output.wbfs]");
            Console.WriteLine("    --bmd2dae  <.bmd>         [output.dae]");
            Console.WriteLine("    --dae2bmd  <.dae>         [output.bmd]  [--mat mat.json] [--texheader tex.json]");
            Console.WriteLine("    --bmd2fbx  <.bmd>         [output folder]   ASCII FBX + GLB を出力 / Outputs ASCII FBX + GLB");
            Console.WriteLine("    --fbx2bmd  <.fbx>         [output.bmd]      skeleton_root 付き FBX を推奨 / FBX with skeleton_root recommended");
            Console.WriteLine("    --bmd2obj  <.bmd>         [output.obj]");
            Console.WriteLine("    --obj2grid <.obj>         [grid.bin] [mapcode.bin] [--cell_size 100] [--flipyz]");
            Console.WriteLine("    --bmgextract <.bmg>       [output.txt]");
            Console.WriteLine("    --bmgpack <.txt/.json>    [output.bmg] [--encoding shift-jis|latin-1|utf-8|utf-16]");
            Console.WriteLine();
            Console.WriteLine("  [GC Disc Notes]");
            Console.WriteLine("    --gcextract outputs files + sys for GameCube discs.");
            Console.WriteLine("    --wiiextract outputs files + sys plus Wii partition metadata.");
            Console.WriteLine("    --gcrebuild rebuilds a valid ISO/GCM from sys + files.");
            Console.WriteLine("    --wiirebuild rebuilds a Wii ISO/WBFS from sys + files + partition metadata.");
            Console.WriteLine("    Rebuilt images may differ in total ISO size and in sys/boot.bin / sys/fst.bin");
            Console.WriteLine("    because file layout and FST offsets are regenerated during rebuild.");
            Console.WriteLine("    In round-trip verification, the extracted files/ contents matched the original.");
            Console.WriteLine();
            Console.WriteLine("  [Context Menu]  (no root privileges required)");
            Console.WriteLine("    --register    Add right-click menu entries to your file manager");
            Console.WriteLine("                  (Nautilus / Dolphin / Nemo / Thunar, per-user settings)");
            Console.WriteLine("    --unregister  Remove right-click menu entries");
            Console.WriteLine("    Folder menu   -> Pack to SZS / Rebuild GC Disc / Rebuild Wii Disc");
            Console.WriteLine("    .iso menu     -> Extract Disc / Convert ISO to WBFS");
            Console.WriteLine("    .bmg menu     -> Extract BMG");
            Console.WriteLine("    .txt/.json    -> Pack BMG");
            Console.WriteLine();
            Console.WriteLine("  External tools (place in the resource/ folder next to this binary):");
            Console.WriteLine("    resource/DiscExtract                -> used by --gcextract / --wiiextract");
            Console.WriteLine("    resource/DiscRebuild                -> used by --gcrebuild / --wiirebuild / --iso2wbfs");
            Console.WriteLine("    resource/BMD_analysis/BMD_analysis  -> used by --bmd2dae / --dae2bmd / --fbx2bmd / --bmd2obj");
            Console.WriteLine("    resource/FBX_analysis/FBX_analysis  -> used by --bmd2fbx");
        }
    }
}
