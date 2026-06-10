using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RARCToolkit.IO;

namespace RARCToolkit.Collision
{
    /// <summary>
    /// Wavefront .obj ファイルを Pikmin 2 のコリジョンファイル
    /// (grid.bin / mapcode.bin) に変換する。
    ///
    /// obj2grid.py by Yoshi2 の C# 移植版。
    /// </summary>
    public sealed class Obj2Grid
    {
        // ─── 内部データ型 ─────────────────────────────────────────────

        private record struct Vec3(float X, float Y, float Z)
        {
            public static Vec3 Zero => new(0f, 0f, 0f);
        }

        // OBJ の 1 面（頂点インデックスは 0 始まり、法線インデックスは null 可）
        private record struct ObjFace(int V1, int V2, int V3,
                                      int? N1, int? N2, int? N3,
                                      byte FloorType);

        // ─── パブリック API ───────────────────────────────────────────

        /// <summary>
        /// OBJ ファイルを grid.bin と mapcode.bin に変換する。
        /// </summary>
        /// <param name="inputObj">入力 .obj ファイルパス</param>
        /// <param name="outputGrid">出力 grid.bin パス（省略時は入力と同じフォルダ）</param>
        /// <param name="outputMapcode">出力 mapcode.bin パス（省略時は入力と同じフォルダ）</param>
        /// <param name="cellSize">グリッドセルサイズ（デフォルト 100）</param>
        /// <param name="flipYZ">Y/Z 軸を入れ替える（Blender 等からの出力に使用）</param>
        public void Convert(
            string inputObj,
            string? outputGrid    = null,
            string? outputMapcode = null,
            int     cellSize      = 100,
            bool    flipYZ        = false)
        {
            string baseDir = Path.GetDirectoryName(inputObj) ?? ".";
            outputGrid    ??= Path.Combine(baseDir, "grid.bin");
            outputMapcode ??= Path.Combine(baseDir, "mapcode.bin");

            Console.WriteLine($"OBJ 解析中: {inputObj}");
            var (vertices, faces) = ReadObj(inputObj, flipYZ);
            Console.WriteLine($"  頂点数: {vertices.Count}, 面数: {faces.Count}");

            if (faces.Any(f => f.V1 >= vertices.Count || f.V2 >= vertices.Count || f.V3 >= vertices.Count))
                throw new InvalidDataException("OBJ ファイルに範囲外の頂点インデックスが含まれています。");

            Console.WriteLine($"grid.bin 書き込み中: {outputGrid}");
            WriteGridBin(outputGrid, vertices, faces, cellSize);

            Console.WriteLine($"mapcode.bin 書き込み中: {outputMapcode}");
            WriteMapcodeBin(outputMapcode, faces);

            Console.WriteLine("コリジョン変換 完了");
        }

        // ─── OBJ パーサー ─────────────────────────────────────────────

        private static (List<Vec3> vertices, List<ObjFace> faces) ReadObj(string path, bool flipYZ)
        {
            var vertices = new List<Vec3>();
            var faces    = new List<ObjFace>();
            byte floorType = 0x00;

            var floorTypeRegex = new Regex(@"^(.*?)(0x[0-9a-fA-F]{2})(.*?)$",
                                           RegexOptions.Compiled | RegexOptions.IgnoreCase);

            foreach (string rawLine in File.ReadLines(path))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#')) continue;

                string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;

                switch (parts[0])
                {
                    case "v":
                    {
                        if (parts.Length < 4) continue;
                        float x = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                        float y = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                        float z = float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture);
                        vertices.Add(flipYZ ? new Vec3(x, z, y) : new Vec3(x, y, z));
                        break;
                    }
                    case "f":
                    {
                        if (parts.Length != 4)
                            throw new InvalidDataException(
                                $"モデルが三角形分割されていません。4 頂点以上の面が含まれています。\n" +
                                $"Blender 等でエクスポート前に三角形化してください。\n" +
                                $"問題の行: {line}");
                        var (v1, n1) = ParseFaceVertex(parts[1]);
                        var (v2, n2) = ParseFaceVertex(parts[2]);
                        var (v3, n3) = ParseFaceVertex(parts[3]);
                        // OBJ は 1 始まりインデックス → 0 始まりに変換
                        faces.Add(new ObjFace(v1 - 1, v2 - 1, v3 - 1, n1, n2, n3, floorType));
                        break;
                    }
                    case "usemtl":
                    {
                        string matName = string.Join(" ", parts[1..]);
                        var m = floorTypeRegex.Match(matName);
                        floorType = m.Success
                            ? System.Convert.ToByte(m.Groups[2].Value, 16)
                            : (byte)0x00;
                        break;
                    }
                }
            }
            return (vertices, faces);
        }

        private static (int vIdx, int? nIdx) ParseFaceVertex(string token)
        {
            string[] split = token.Split('/');
            int v = int.Parse(split[0]);
            int? n = split.Length >= 3 && split[2].Length > 0 ? int.Parse(split[2]) : (int?)null;
            return (v, n);
        }

        // ─── grid.bin 書き込み ────────────────────────────────────────

        private static void WriteGridBin(string path, List<Vec3> vertices, List<ObjFace> faces, int cellSize)
        {
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var w  = new EndianBinaryWriter(fs);

            // ── 頂点 ──
            w.Write(vertices.Count);
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            foreach (var v in vertices)
            {
                w.WriteFloat(v.X); w.WriteFloat(v.Y); w.WriteFloat(v.Z);
                if (v.X < minX) minX = v.X; if (v.X > maxX) maxX = v.X;
                if (v.Y < minY) minY = v.Y; if (v.Y > maxY) maxY = v.Y;
                if (v.Z < minZ) minZ = v.Z; if (v.Z > maxZ) maxZ = v.Z;
            }

            // ── 面（面数のプレースホルダーを書いてから面データを書く）──
            long faceCountOffset = fs.Position;
            w.Write(faces.Count);   // 後でパッチする可能性あり（互換性のため原本の挙動を踏襲）

            var precomputedFaces = new List<FaceWriteData>(faces.Count);

            for (int i = 0; i < faces.Count; i++)
            {
                ObjFace face = faces[i];
                Vec3 p1 = vertices[face.V1];
                Vec3 p2 = vertices[face.V2];
                Vec3 p3 = vertices[face.V3];

                // v1→v2, v1→v3 の外積で法線を計算
                Vec3 v1tov2 = Sub(p2, p1);
                Vec3 v1tov3 = Sub(p3, p1);
                Vec3 v2tov3 = Sub(p3, p2);
                Vec3 v3tov1 = Sub(p1, p3);

                Vec3 crossNorm = Cross(v1tov2, v1tov3);
                bool degenerate = crossNorm.X == 0f && crossNorm.Y == 0f && crossNorm.Z == 0f;

                FaceWriteData fd;
                if (degenerate)
                {
                    fd = new FaceWriteData(face.V1, face.V3, face.V2,
                        Vec3.Zero, 0f, Vec3.Zero, 0f, Vec3.Zero, 0f, Vec3.Zero, 0f);
                }
                else
                {
                    Vec3 norm = Normalize(crossNorm);
                    Vec3 tan1 = Normalize(Cross(v1tov2, norm));
                    Vec3 tan2 = Normalize(Cross(v2tov3, norm));
                    Vec3 tan3 = Normalize(Cross(v3tov1, norm));

                    float midX = (p1.X + p2.X + p3.X) / 3f;
                    float midY = (p1.Y + p2.Y + p3.Y) / 3f;
                    float midZ = (p1.Z + p2.Z + p3.Z) / 3f;

                    float a = norm.X * midX + norm.Y * midY + norm.Z * midZ;
                    float b = tan1.X * p1.X + tan1.Y * p1.Y + tan1.Z * p1.Z;
                    float c = tan2.X * p2.X + tan2.Y * p2.Y + tan2.Z * p2.Z;
                    float d = tan3.X * p3.X + tan3.Y * p3.Y + tan3.Z * p3.Z;

                    // 書き込み順は v1, v3, v2 （Python 原本どおり v3/v2 を入れ替え）
                    fd = new FaceWriteData(face.V1, face.V3, face.V2,
                        norm, a, tan1, b, tan2, c, tan3, d);
                }

                precomputedFaces.Add(fd);

                w.Write(fd.V1); w.Write(fd.V2); w.Write(fd.V3);
                WriteFaceVec(w, fd.Norm); w.WriteFloat(fd.A);
                WriteFaceVec(w, fd.Tan1); w.WriteFloat(fd.B);
                WriteFaceVec(w, fd.Tan2); w.WriteFloat(fd.C);
                WriteFaceVec(w, fd.Tan3); w.WriteFloat(fd.D);
            }

            // ── グリッド境界の計算 ──
            // Pikmin 2 のマップ座標は ±6000 にクランプ
            minX = MathF.Max(-6000f, minX); maxX = MathF.Min(6000f, maxX);
            minZ = MathF.Max(-6000f, minZ); maxZ = MathF.Min(6000f, maxZ);

            float startX = MathF.Floor(minX / cellSize) * cellSize;
            float startZ = MathF.Floor(minZ / cellSize) * cellSize;
            float endX   = MathF.Ceiling(maxX / cellSize) * cellSize;
            float endZ   = MathF.Ceiling(maxZ / cellSize) * cellSize;

            int gridSizeX = (int)((endX - startX) / cellSize);
            int gridSizeZ = (int)((endZ - startZ) / cellSize);

            Console.WriteLine($"  コリジョン境界: X [{startX} .. {endX}]  Z [{startZ} .. {endZ}]");
            Console.WriteLine($"  グリッドサイズ: {gridSizeX} x {gridSizeZ}  セル: {cellSize}");

            // ── グリッド境界ヘッダー ──
            w.WriteFloat(startX); w.WriteFloat(minY); w.WriteFloat(startZ);
            w.WriteFloat(endX);   w.WriteFloat(maxY); w.WriteFloat(endZ);
            w.Write(gridSizeX);   w.Write(gridSizeZ);
            w.WriteFloat(cellSize); w.WriteFloat(cellSize);

            // ── グリッド分割（四分木）──
            var triangles = new List<TriEntry>(faces.Count);
            for (int i = 0; i < faces.Count; i++)
                triangles.Add(new TriEntry(i, faces[i].V1, faces[i].V2, faces[i].V3));

            var grid = new Dictionary<(int, int), List<int>>();
            SubdivideGrid(startX, startZ, 0, gridSizeX, 0, gridSizeZ,
                          cellSize, triangles, vertices, grid);

            // ── グリッドセルの書き込み ──
            for (int ix = 0; ix < gridSizeX; ix++)
            {
                for (int iz = 0; iz < gridSizeZ; iz++)
                {
                    if (grid.TryGetValue((ix, iz), out var group))
                    {
                        w.Write(group.Count);
                        foreach (int fi in group) w.Write(fi);
                    }
                    else
                    {
                        w.Write(0);
                    }
                }
            }
        }

        // ─── mapcode.bin 書き込み ─────────────────────────────────────

        private static void WriteMapcodeBin(string path, List<ObjFace> faces)
        {
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var w  = new EndianBinaryWriter(fs);
            w.Write(faces.Count);
            foreach (var f in faces)
                fs.WriteByte(f.FloorType);
        }

        // ─── 四分木によるグリッド分割 ───────────────────────────────────

        private static void SubdivideGrid(
            float startX, float startZ,
            int gxStart, int gxEnd, int gzStart, int gzEnd,
            float cellSize,
            List<TriEntry> triangles,
            List<Vec3> vertices,
            Dictionary<(int, int), List<int>> result)
        {
            // 基底ケース: 1×1 セル
            if (gxStart == gxEnd - 1 && gzStart == gzEnd - 1)
            {
                if (triangles.Count > 0)
                    result[(gxStart, gzStart)] = triangles.Select(t => t.Idx).ToList();
                return;
            }

            int halfX = (gxStart + gxEnd) / 2;
            int halfZ = (gzStart + gzEnd) / 2;

            // スキップすべき縮退した四分割
            var skip = new HashSet<int>();
            if (gxStart == halfX) { skip.Add(0); skip.Add(2); }
            if (halfX == gxEnd)   { skip.Add(1); skip.Add(3); }
            if (gzStart == halfZ) { skip.Add(0); skip.Add(1); }
            if (halfZ == gzEnd)   { skip.Add(2); skip.Add(3); }

            // 四分割の定義: (quadrant, xStart, xEnd, zStart, zEnd)
            (int q, int sx, int ex, int sz, int ez)[] coords =
            {
                (0, gxStart, halfX,  gzStart, halfZ),
                (1, halfX,   gxEnd,  gzStart, halfZ),
                (2, gxStart, halfX,  halfZ,   gzEnd),
                (3, halfX,   gxEnd,  halfZ,   gzEnd),
            };

            // 三角形を各四分割に振り分け
            var quadrants = new List<TriEntry>[4];
            for (int i = 0; i < 4; i++) quadrants[i] = new List<TriEntry>();

            foreach (var tri in triangles)
            {
                Vec3 p1 = vertices[tri.V1];
                Vec3 p2 = vertices[tri.V2];
                Vec3 p3 = vertices[tri.V3];

                foreach (var (q, sx, ex, sz, ez) in coords)
                {
                    if (skip.Contains(q)) continue;
                    float areaSizeX = (ex - sx) * cellSize;
                    float areaSizeZ = (ez - sz) * cellSize;
                    float midX = startX + sx * cellSize + areaSizeX / 2f;
                    float midZ = startZ + sz * cellSize + areaSizeZ / 2f;

                    if (Collides(p1, p2, p3, midX, midZ, areaSizeX, areaSizeZ))
                        quadrants[q].Add(tri);
                }
            }

            // 再帰処理
            foreach (var (q, sx, ex, sz, ez) in coords)
            {
                if (!skip.Contains(q))
                    SubdivideGrid(startX, startZ, sx, ex, sz, ez,
                                  cellSize, quadrants[q], vertices, result);
            }
        }

        // ─── 数学ユーティリティ ─────────────────────────────────────────

        private static bool Collides(Vec3 p1, Vec3 p2, Vec3 p3,
                                     float boxMidX, float boxMidZ,
                                     float boxSizeX, float boxSizeZ)
        {
            float minX = MathF.Min(p1.X, MathF.Min(p2.X, p3.X)) - boxMidX;
            float maxX = MathF.Max(p1.X, MathF.Max(p2.X, p3.X)) - boxMidX;
            float minZ = MathF.Min(p1.Z, MathF.Min(p2.Z, p3.Z)) - boxMidZ;
            float maxZ = MathF.Max(p1.Z, MathF.Max(p2.Z, p3.Z)) - boxMidZ;
            float hx = boxSizeX / 2f, hz = boxSizeZ / 2f;
            return !(maxX < -hx || minX > hx || maxZ < -hz || minZ > hz);
        }

        private static Vec3 Sub(Vec3 a, Vec3 b) =>
            new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        private static Vec3 Cross(Vec3 a, Vec3 b) =>
            new(a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X);

        private static Vec3 Normalize(Vec3 v)
        {
            float n = MathF.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
            return n < 1e-8f ? Vec3.Zero : new(v.X / n, v.Y / n, v.Z / n);
        }

        private static void WriteFaceVec(EndianBinaryWriter w, Vec3 v)
        {
            w.WriteFloat(v.X); w.WriteFloat(v.Y); w.WriteFloat(v.Z);
        }

        // ─── 内部ヘルパー型 ─────────────────────────────────────────────

        private record struct TriEntry(int Idx, int V1, int V2, int V3);

        private record struct FaceWriteData(
            int V1, int V2, int V3,
            Vec3 Norm, float A,
            Vec3 Tan1,  float B,
            Vec3 Tan2,  float C,
            Vec3 Tan3,  float D);
    }
}
