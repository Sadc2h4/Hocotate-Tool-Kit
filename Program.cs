using System;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using RARCToolkit.Collision;
using RARCToolkit.Conversion;
using RARCToolkit.IO;
using RARCToolkit.RARC;

namespace RARCToolkit
{
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
            Console.WriteLine();
        }

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
                Console.WriteLine("  1. Drag & drop a file or folder directly onto this exe.");
                Console.WriteLine("  2. Call from a batch file or program with arguments.");
                Console.WriteLine("──────────────────────────────────────────────────────────────");
                Console.WriteLine();
                Console.WriteLine("Press any key to close...");
                Console.ReadKey();
                return 1;
            }

            // Drag & drop: first arg is a path, not a flag
            if (!args[0].StartsWith("-") && !args[0].StartsWith("/"))
                return AutoDetect(args[0]);

            string mode = args[0].TrimStart('-', '/').ToLower();

            // register / unregister do not need a second argument
            if (mode == "register")   return DoRegister();
            if (mode == "unregister") return DoUnregister();

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
                    "gcrebuild"=> DoGcRebuild(input, OptArg(args, 2)),
                    "bmd2dae"  => DoBmd2Dae(input, OptArg(args, 2)),
                    "dae2bmd"  => DoDae2Bmd(args),
                    "bmd2fbx"  => DoBmd2Fbx(input, OptArg(args, 2)),
                    "bmd2obj"  => DoBmd2Obj(input, OptArg(args, 2)),
                    "obj2grid" => DoObj2Grid(args),
                    _ => UnknownMode(args[0]),
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"\nError: {ex.Message}");
                return 1;
            }
        }

        // ── Drag & drop auto-detect ───────────────────────────────────────────

        static int AutoDetect(string path)
        {
            PrintBanner();
            Console.WriteLine($"Input: {path}");
            Console.WriteLine();

            int result;
            try
            {
                if (Directory.Exists(path))
                {
                    if (LooksLikeGameCubeDiscRoot(path))
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
                        ".iso" or ".gcm" => RunGcExtract(path),
                        ".bmd" or ".bdl" => RunBmdAll(path),
                        ".dae"           => RunDae2Bmd(path),
                        ".obj"           => RunObj2Grid(path),
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
            Console.WriteLine("Press any key to close...");
            Console.ReadKey();
            return result;
        }

        static int RunExtract(string path)
        {
            Console.WriteLine($"[{Path.GetExtension(path).ToUpper()}] -> Extract");
            return DoExtract(path, null);
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

        static int RunGcExtract(string path)
        {
            Console.WriteLine("[ISO/GCM] -> Extract GameCube disc");
            return DoGcExtract(path, null);
        }

        static int RunGcRebuild(string path)
        {
            Console.WriteLine("[Folder] -> Rebuild GameCube disc");
            return DoGcRebuild(path, null);
        }

        static int RunDae2Bmd(string path)
        {
            Console.WriteLine("[DAE] -> BMD conversion");
            return DoDae2Bmd(new[] { "--dae2bmd", path });
        }

        static int RunObj2Grid(string path)
        {
            Console.WriteLine("[OBJ] -> grid.bin + mapcode.bin");
            return DoObj2Grid(new[] { "--obj2grid", path });
        }

        static int UnknownDrop(string ext)
        {
            Console.Error.WriteLine($"Unsupported file type: {ext}");
            Console.Error.WriteLine("Supported: folder, .arc, .szs, .iso, .gcm, .bmd, .bdl, .dae, .obj");
            return 1;
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

        // ── RARC pack / extract ───────────────────────────────────────────────

        static int DoPack(string folderPath, string? outputPath)
        {
            folderPath = folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            RequireDirectory(folderPath);
            outputPath ??= SiblingFile(folderPath, ".arc");
            Console.WriteLine($"Pack: {folderPath} -> {outputPath}");
            PackFolder(folderPath, outputPath);
            Console.WriteLine("Done.");
            return 0;
        }

        static int DoSzs(string folderPath, string? outputPath)
        {
            folderPath = folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            RequireDirectory(folderPath);
            outputPath ??= SiblingFile(folderPath, ".szs");
            Console.WriteLine($"SZS pack: {folderPath} -> {outputPath}");
            PackFolder(folderPath, outputPath);
            Console.WriteLine("Done.");
            return 0;
        }

        static int DoExtract(string inputPath, string? outputDir)
        {
            RequireFile(inputPath);
            outputDir ??= Path.Combine(
                Path.GetDirectoryName(inputPath) ?? ".",
                Path.GetFileNameWithoutExtension(inputPath));
            Console.WriteLine($"Extract: {inputPath} -> {outputDir}");
            new RARCExtractor().Extract(inputPath, outputDir);
            return 0;
        }

        static int DoGcExtract(string inputPath, string? outputDir)
        {
            RequireFile(inputPath);
            Console.WriteLine($"GC extract: {inputPath} -> {outputDir ?? "(auto)"}");
            return DiscExtractConvert.ExtractGameCubeDisc(inputPath, outputDir);
        }

        static int DoGcRebuild(string inputFolder, string? outputPath)
        {
            RequireDirectory(inputFolder);
            Console.WriteLine($"GC rebuild: {inputFolder} -> {outputPath ?? "(auto)"}");
            return DiscRebuildConvert.RebuildGameCubeDisc(inputFolder, outputPath);
        }

        // ── BMD_analysis wrapper ──────────────────────────────────────────────

        static int DoBmd2Dae(string inputBmd, string? outputDae)
        {
            RequireFile(inputBmd);
            if (string.IsNullOrEmpty(outputDae))
            {
                string dir       = Path.GetDirectoryName(inputBmd) ?? ".";
                string name      = Path.GetFileNameWithoutExtension(inputBmd);
                string outFolder = Path.Combine(dir, name);
                Directory.CreateDirectory(outFolder);
                outputDae        = Path.Combine(outFolder, name + ".dae");
            }
            return SuperBMDConvert.Bmd2Dae(inputBmd, outputDae);
        }

        static int DoDae2Bmd(string[] args)
        {
            RequireFile(args[1]);
            string? output = OptArg(args, 2);
            string? mat    = GetFlag(args, "--mat");
            string? texhdr = GetFlag(args, "--texheader");
            return SuperBMDConvert.Dae2Bmd(args[1], output, mat, texhdr);
        }

        static int DoBmd2Fbx(string inputBmd, string? outputDir)
        {
            RequireFile(inputBmd);
            return SuperBMDConvert.Bmd2Fbx(inputBmd, outputDir);
        }

        static int DoBmd2Obj(string inputBmd, string? outputObj)
        {
            RequireFile(inputBmd);
            if (string.IsNullOrEmpty(outputObj))
            {
                string dir       = Path.GetDirectoryName(inputBmd) ?? ".";
                string name      = Path.GetFileNameWithoutExtension(inputBmd);
                string outFolder = Path.Combine(dir, name);
                Directory.CreateDirectory(outFolder);
                outputObj        = Path.Combine(outFolder, name + ".obj");
            }
            return SuperBMDConvert.Bmd2Obj(inputBmd, outputObj);
        }

        // ── obj2grid ─────────────────────────────────────────────────────────

        static int DoObj2Grid(string[] args)
        {
            string inputObj = args[1];
            RequireFile(inputObj);

            string? outputGrid    = OptArg(args, 2);
            string? outputMapcode = OptArg(args, 3);
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

        // ── Context menu register / unregister ───────────────────────────────

        static int DoRegister()
        {
            string exePath = Environment.ProcessPath
                          ?? System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName!;

            var entries = new (string key, string label)[]
            {
                (@"Software\Classes\.arc\shell\HocotateToolkit", "Hocotate Toolkit - Extract"),
                (@"Software\Classes\.szs\shell\HocotateToolkit", "Hocotate Toolkit - Extract"),
                (@"Software\Classes\.iso\shell\HocotateToolkit", "Hocotate Toolkit - Extract GC Disc"),
                (@"Software\Classes\.gcm\shell\HocotateToolkit", "Hocotate Toolkit - Extract GC Disc"),
                (@"Software\Classes\.bmd\shell\HocotateToolkit", "Hocotate Toolkit - Convert BMD"),
                (@"Software\Classes\.bdl\shell\HocotateToolkit", "Hocotate Toolkit - Convert BMD"),
                (@"Software\Classes\.dae\shell\HocotateToolkit", "Hocotate Toolkit - DAE to BMD"),
                (@"Software\Classes\.obj\shell\HocotateToolkit", "Hocotate Toolkit - OBJ to grid.bin"),
            };

            try
            {
                foreach (var (key, label) in entries)
                {
                    using var shellKey = Registry.CurrentUser.CreateSubKey(key);
                    shellKey.SetValue("", label);
                    shellKey.SetValue("Icon", $"\"{exePath}\"");
                    using var cmdKey = Registry.CurrentUser.CreateSubKey(key + @"\command");
                    string command = key.Contains(@"\.iso\") || key.Contains(@"\.gcm\")
                        ? $"\"{exePath}\" --gcextract \"%1\""
                        : $"\"{exePath}\" \"%1\"";
                    cmdKey.SetValue("", command);
                }

                using var dirKey = Registry.CurrentUser.CreateSubKey(
                    @"Software\Classes\Directory\shell\HocotateToolkit");
                dirKey.SetValue("", "Hocotate Toolkit - Pack to SZS / Rebuild GC Disc");
                dirKey.SetValue("Icon", $"\"{exePath}\"");
                using var dirCmd = Registry.CurrentUser.CreateSubKey(
                    @"Software\Classes\Directory\shell\HocotateToolkit\command");
                dirCmd.SetValue("", $"\"{exePath}\" \"%1\"");

                Console.WriteLine("Context menu registered successfully.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Registration failed: {ex.Message}");
                return 1;
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to close...");
            Console.ReadKey();
            return 0;
        }

        static int DoUnregister()
        {
            var keys = new[]
            {
                @"Software\Classes\.arc\shell\HocotateToolkit",
                @"Software\Classes\.szs\shell\HocotateToolkit",
                @"Software\Classes\.iso\shell\HocotateToolkit",
                @"Software\Classes\.gcm\shell\HocotateToolkit",
                @"Software\Classes\.bmd\shell\HocotateToolkit",
                @"Software\Classes\.bdl\shell\HocotateToolkit",
                @"Software\Classes\.dae\shell\HocotateToolkit",
                @"Software\Classes\.obj\shell\HocotateToolkit",
                @"Software\Classes\Directory\shell\HocotateToolkit",
            };

            try
            {
                foreach (var key in keys)
                    Registry.CurrentUser.DeleteSubKeyTree(key, throwOnMissingSubKey: false);

                Console.WriteLine("Context menu unregistered successfully.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unregistration failed: {ex.Message}");
                return 1;
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to close...");
            Console.ReadKey();
            return 0;
        }

        // ── Common helpers ────────────────────────────────────────────────────

        static void PackFolder(string folderPath, string outputPath)
        {
            VirtualFolder root = BuildVirtualFolder(folderPath, isRoot: true);
            using var fs     = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            using var writer = new EndianBinaryWriter(fs);
            new RARCPacker().Pack(root, writer);
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
            Console.WriteLine("  The following applications and authors were referenced for each feature.");
            Console.WriteLine("  Thank you for sharing this information.");
            Console.WriteLine();
            Console.WriteLine("  Operation   Reference Application   Author");
            Console.WriteLine("  --pack      RARCPack                Yoshi2");
            Console.WriteLine("  --szs       RARCPack                Yoshi2");
            Console.WriteLine("  --extract   ARCExtractor            cuzitsjonny");
            Console.WriteLine("  --gcextract DiscExtract             jordan-woyak (Dolphin / DolphinTool reference)");
            Console.WriteLine("  --gcrebuild DiscRebuild             jordan-woyak (Dolphin / DolphinTool reference)");
            Console.WriteLine("  --bmd2dae   SuperBMD_2.4.2.1_RC     RenolY2");
            Console.WriteLine("  --bmd2fbx   MeltyTool               MeltyPlayer");
            Console.WriteLine("  --bmd2obj   obj2grid                RenolY2");
            Console.WriteLine("  --dae2bmd   SuperBMD_2.4.2.1_RC     RenolY2");
            Console.WriteLine("  --obj2grid  obj2grid                RenolY2");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine();
            Console.WriteLine("  [Drag & Drop]  Drop a file or folder directly onto this exe:");
            Console.WriteLine("    Folder           -> Pack to SZS (.szs)");
            Console.WriteLine("                      -> Rebuild GC disc if sys + files are present");
            Console.WriteLine("    .arc / .szs      -> Extract");
            Console.WriteLine("    .iso / .gcm      -> Extract GameCube disc (files + sys)");
            Console.WriteLine("    .bmd / .bdl      -> Convert to DAE + FBX + OBJ (batch)");
            Console.WriteLine("    .dae             -> Convert to BMD");
            Console.WriteLine("    .obj             -> Generate grid.bin + mapcode.bin");
            Console.WriteLine();
            Console.WriteLine("  [Command Line]");
            Console.WriteLine("    --pack     <folder>       [output.arc]");
            Console.WriteLine("    --szs      <folder>       [output.szs]");
            Console.WriteLine("    --extract  <.arc/.szs>    [output folder]");
            Console.WriteLine("    --gcextract <.iso/.gcm>   [output folder]");
            Console.WriteLine("    --gcrebuild <folder>      [output.iso/.gcm]");
            Console.WriteLine("    --bmd2dae  <.bmd>         [output.dae]");
            Console.WriteLine("    --dae2bmd  <.dae>         [output.bmd]  [--mat mat.json] [--texheader tex.json]");
            Console.WriteLine("    --bmd2fbx  <.bmd>         [output folder]   * requires FBX_analysis.exe");
            Console.WriteLine("    --bmd2obj  <.bmd>         [output.obj]");
            Console.WriteLine("    --obj2grid <.obj>         [grid.bin] [mapcode.bin] [--cell_size 100] [--flipyz]");
            Console.WriteLine();
            Console.WriteLine("  [GC Disc Notes]");
            Console.WriteLine("    --gcextract outputs files + sys for GameCube discs.");
            Console.WriteLine("    --gcrebuild rebuilds a valid ISO/GCM from sys + files.");
            Console.WriteLine("    Rebuilt images may differ in total ISO size and in sys\\boot.bin / sys\\fst.bin");
            Console.WriteLine("    because file layout and FST offsets are regenerated during rebuild.");
            Console.WriteLine("    In round-trip verification, the extracted files\\ contents matched the original.");
            Console.WriteLine();
            Console.WriteLine("  [Context Menu]  (no admin rights required)");
            Console.WriteLine("    --register    Add right-click menu entries for supported file types");
            Console.WriteLine("    --unregister  Remove right-click menu entries");
            Console.WriteLine();
            Console.WriteLine("  External tools (place in the resource\\ folder next to this exe):");
            Console.WriteLine("    resource\\DiscExtract.exe  -> used by --gcextract");
            Console.WriteLine("    resource\\DiscRebuild.exe  -> used by --gcrebuild");
            Console.WriteLine("    resource\\BMD_analysis.exe  -> used by --bmd2dae / --dae2bmd / --bmd2obj");
            Console.WriteLine("    resource\\FBX_analysis.exe  -> used by --bmd2fbx");
        }
    }
}
