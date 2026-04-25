using System.Collections.Generic;

namespace RARCToolkit.RARC
{
    /// <summary>
    /// パック処理用の仮想フォルダ構造。
    /// RarcPack-master / VirtualFolder を GameFormatReader 依存なしに移植。
    /// </summary>
    public class VirtualFolder
    {
        public string Name = string.Empty;

        /// <summary>
        /// RARC ノードの 4 文字タイプ識別子（例: "ROOT", "res "）。
        /// </summary>
        public string NodeName = string.Empty;

        public List<VirtualFolder> Subdirs = new();
        public List<FileData> Files = new();
    }

    public class FileData
    {
        public string Name = string.Empty;
        public byte[] Data = Array.Empty<byte>();
    }
}
