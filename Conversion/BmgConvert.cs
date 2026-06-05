using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace RARCToolkit.Conversion
{
    /// <summary>
    /// Nintendo BMG メッセージファイルとJSONテキストの相互変換を行う。
    /// </summary>
    public static class BmgConvert
    {
        //-------------------------------------------------------------------------------
        // Shift-JISなどのコードページ文字コードを有効化する処理
        //-------------------------------------------------------------------------------
        static BmgConvert()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        //-------------------------------------------------------------------------------
        // BMGファイルをJSONテキストへ展開する処理
        //-------------------------------------------------------------------------------
        public static void Extract(string inputBmg, string outputText)
        {
            BmgFile bmg = BmgFile.Read(File.ReadAllBytes(inputBmg));
            var jsonItems = new List<Dictionary<string, object?>>();
            var metadata = new Dictionary<string, object?>
            {
                ["Attribute Length"] = bmg.AttributeLength,
                ["Unknown MID1 Value"] = bmg.Mid1Value.ToString("x", CultureInfo.InvariantCulture),
                ["Encoding"] = bmg.EncodingName,
                ["Endian"] = bmg.BigEndian ? "big" : "little",
            };
            jsonItems.Add(metadata);

            for (int i = 0; i < bmg.Messages.Count; i++)
            {
                BmgMessage message = bmg.Messages[i];
                jsonItems.Add(new Dictionary<string, object?>
                {
                    ["ID"] = $"{message.Id}, {message.SubId}",
                    ["index"] = "0x" + i.ToString("x", CultureInfo.InvariantCulture),
                    ["attributes"] = ToHex(message.Attributes),
                    ["text"] = message.Text.Split('\n'),
                });
            }

            foreach (BmgSection section in bmg.UnknownSections)
            {
                jsonItems.Add(new Dictionary<string, object?>
                {
                    ["Section"] = section.Magic,
                    ["Data"] = ToHex(section.Data),
                });
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(outputText, JsonSerializer.Serialize(jsonItems, options), new UTF8Encoding(false));
        }

        //-------------------------------------------------------------------------------
        // JSONテキストをBMGファイルへパックする処理
        //-------------------------------------------------------------------------------
        public static void Pack(string inputText, string outputBmg, string? encodingOverride)
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(inputText, DetectTextEncoding(inputText)));
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("BMG text must be a JSON array.");

            JsonElement[] items = document.RootElement.EnumerateArray().ToArray();
            if (items.Length == 0)
                throw new InvalidDataException("BMG text has no metadata or messages.");

            JsonElement metadata = items[0];
            int attributeLength = GetInt(metadata, "Attribute Length", 8);
            ushort mid1Value = (ushort)GetHexOrInt(metadata, "Unknown MID1 Value", 0x1001);
            string encodingName = encodingOverride ?? GetString(metadata, "Encoding", "shift-jis");
            bool bigEndian = !GetString(metadata, "Endian", "big").Equals("little", StringComparison.OrdinalIgnoreCase);

            var bmg = new BmgFile
            {
                BigEndian = bigEndian,
                EncodingByte = EncodingByteFromName(encodingName),
                AttributeLength = attributeLength,
                Mid1Value = mid1Value,
            };

            foreach (JsonElement item in items.Skip(1))
            {
                if (item.TryGetProperty("Section", out JsonElement sectionName))
                {
                    bmg.UnknownSections.Add(new BmgSection(
                        sectionName.GetString() ?? "UNKN",
                        FromHex(GetString(item, "Data", ""))));
                    continue;
                }

                string idText = GetString(item, "ID", "0, 0");
                string[] idParts = idText.Split(',');
                uint id = idParts.Length > 0 ? uint.Parse(idParts[0].Trim(), CultureInfo.InvariantCulture) : 0;
                byte subId = idParts.Length > 1 ? byte.Parse(idParts[1].Trim(), CultureInfo.InvariantCulture) : (byte)0;
                string text = ReadMessageText(item);
                byte[] attributes = FromHex(GetString(item, "attributes", ""));
                if (attributes.Length < attributeLength - 4)
                    Array.Resize(ref attributes, attributeLength - 4);

                bmg.Messages.Add(new BmgMessage(id, subId, attributes, text));
            }

            File.WriteAllBytes(outputBmg, bmg.Write());
        }

        //-------------------------------------------------------------------------------
        // JSON内のtext要素を文字列へ変換する処理
        //-------------------------------------------------------------------------------
        private static string ReadMessageText(JsonElement item)
        {
            if (!item.TryGetProperty("text", out JsonElement textElement))
                return "";

            if (textElement.ValueKind == JsonValueKind.Array)
                return string.Join("\n", textElement.EnumerateArray().Select(x => x.GetString() ?? ""));

            return textElement.GetString() ?? "";
        }

        //-------------------------------------------------------------------------------
        // BMG入力テキストのBOMから文字コードを判定する処理
        //-------------------------------------------------------------------------------
        private static Encoding DetectTextEncoding(string path)
        {
            byte[] bom = File.ReadAllBytes(path).Take(4).ToArray();
            if (bom.Length >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
                return new UTF8Encoding(true);
            if (bom.Length >= 2 && bom[0] == 0xFF && bom[1] == 0xFE)
                return Encoding.Unicode;
            if (bom.Length >= 2 && bom[0] == 0xFE && bom[1] == 0xFF)
                return Encoding.BigEndianUnicode;

            return new UTF8Encoding(false);
        }

        //-------------------------------------------------------------------------------
        // BMG文字コード名からヘッダー値を返す処理
        //-------------------------------------------------------------------------------
        private static byte EncodingByteFromName(string name)
        {
            return name.ToLowerInvariant() switch
            {
                "undefined" => 0,
                "cp1252" or "latin-1" or "latin1" => 1,
                "utf-16" or "utf16" => 2,
                "shift-jis" or "shift_jis" or "sjis" => 3,
                "utf-8" or "utf8" => 4,
                _ => throw new InvalidDataException($"Unsupported BMG encoding: {name}"),
            };
        }

        //-------------------------------------------------------------------------------
        // JSON文字列を取得する処理
        //-------------------------------------------------------------------------------
        private static string GetString(JsonElement element, string name, string fallback)
            => element.TryGetProperty(name, out JsonElement value) ? value.GetString() ?? fallback : fallback;

        //-------------------------------------------------------------------------------
        // JSON数値を取得する処理
        //-------------------------------------------------------------------------------
        private static int GetInt(JsonElement element, string name, int fallback)
            => element.TryGetProperty(name, out JsonElement value) && value.TryGetInt32(out int result) ? result : fallback;

        //-------------------------------------------------------------------------------
        // JSONの16進文字列または数値を取得する処理
        //-------------------------------------------------------------------------------
        private static int GetHexOrInt(JsonElement element, string name, int fallback)
        {
            if (!element.TryGetProperty(name, out JsonElement value))
                return fallback;

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number))
                return number;

            string text = value.GetString() ?? "";
            return int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int hex) ? hex : fallback;
        }

        //-------------------------------------------------------------------------------
        // バイト列を16進文字列へ変換する処理
        //-------------------------------------------------------------------------------
        private static string ToHex(byte[] data)
            => Convert.ToHexString(data).ToLowerInvariant();

        //-------------------------------------------------------------------------------
        // 16進文字列をバイト列へ変換する処理
        //-------------------------------------------------------------------------------
        private static byte[] FromHex(string text)
        {
            text = new string(text.Where(Uri.IsHexDigit).ToArray());
            if (text.Length == 0)
                return Array.Empty<byte>();
            if ((text.Length & 1) != 0)
                throw new InvalidDataException("Hex string has an odd length.");

            return Convert.FromHexString(text);
        }

        private sealed class BmgFile
        {
            public bool BigEndian { get; set; } = true;
            public byte EncodingByte { get; set; } = 3;
            public int AttributeLength { get; set; } = 8;
            public ushort Mid1Value { get; set; } = 0x1001;
            public List<BmgMessage> Messages { get; } = new();
            public List<BmgSection> UnknownSections { get; } = new();
            public string EncodingName => EncodingByte switch
            {
                0 => "undefined",
                1 => "latin-1",
                2 => "utf-16",
                3 => "shift-jis",
                4 => "utf-8",
                _ => "shift-jis",
            };

            //-------------------------------------------------------------------------------
            // BMGバイナリを読み込む処理
            //-------------------------------------------------------------------------------
            public static BmgFile Read(byte[] data)
            {
                if (data.Length < 0x20)
                    throw new InvalidDataException("BMG file is too small.");

                string magic = Encoding.ASCII.GetString(data, 0, 8);
                bool bigEndian = magic switch
                {
                    "MESGbmg1" => true,
                    "MESG1gmb" => false,
                    _ => throw new InvalidDataException($"Input file is not a BMG file: {magic}"),
                };

                var bmg = new BmgFile
                {
                    BigEndian = bigEndian,
                    EncodingByte = data[0x10],
                };

                uint sectionCount = ReadUInt32(data, 0x0C, bigEndian);
                int offset = 0x20;
                byte[] infData = Array.Empty<byte>();
                byte[] datData = Array.Empty<byte>();
                byte[] midData = Array.Empty<byte>();

                for (int i = 0; i < sectionCount; i++)
                {
                    string sectionMagic = ReadMagic(data, offset, bigEndian);
                    uint sectionSize = ReadUInt32(data, offset + 4, bigEndian);
                    byte[] sectionData = data[(offset + 8)..(offset + (int)sectionSize)];

                    switch (sectionMagic)
                    {
                        case "INF1":
                            infData = sectionData;
                            break;
                        case "DAT1":
                            datData = sectionData;
                            break;
                        case "MID1":
                            midData = sectionData;
                            break;
                        default:
                            bmg.UnknownSections.Add(new BmgSection(sectionMagic, sectionData));
                            break;
                    }

                    offset += (int)sectionSize;
                }

                bmg.ReadMessages(infData, datData, midData);
                return bmg;
            }

            //-------------------------------------------------------------------------------
            // INF1とDAT1とMID1からメッセージを読み取る処理
            //-------------------------------------------------------------------------------
            private void ReadMessages(byte[] infData, byte[] datData, byte[] midData)
            {
                if (infData.Length < 8 || datData.Length < 1)
                    throw new InvalidDataException("BMG file does not contain valid INF1/DAT1 sections.");

                int messageCount = ReadUInt16(infData, 0, BigEndian);
                AttributeLength = ReadUInt16(infData, 2, BigEndian);
                if (midData.Length >= 4)
                    Mid1Value = ReadUInt16(midData, 2, BigEndian);

                for (int i = 0; i < messageCount; i++)
                {
                    int entryOffset = 8 + i * AttributeLength;
                    uint textOffset = ReadUInt32(infData, entryOffset, BigEndian);
                    byte[] attributes = infData[(entryOffset + 4)..(entryOffset + AttributeLength)];
                    (uint id, byte subId) = ReadMessageId(midData, i);
                    string text = DecodeMessage(datData, (int)textOffset);
                    Messages.Add(new BmgMessage(id, subId, attributes, text));
                }
            }

            //-------------------------------------------------------------------------------
            // MID1からメッセージIDを読み取る処理
            //-------------------------------------------------------------------------------
            private (uint Id, byte SubId) ReadMessageId(byte[] midData, int index)
            {
                int offset = 8 + index * 4;
                if (midData.Length < offset + 4)
                    return ((uint)index, 0);

                uint value = ReadUInt32(midData, offset, BigEndian);
                return (value >> 8, (byte)value);
            }

            //-------------------------------------------------------------------------------
            // BMGバイナリを書き出す処理
            //-------------------------------------------------------------------------------
            public byte[] Write()
            {
                var output = new MemoryStream();
                output.Write(Encoding.ASCII.GetBytes(BigEndian ? "MESGbmg1" : "MESG1gmb"));
                WriteUInt32(output, 0, BigEndian);
                WriteUInt32(output, (uint)(3 + UnknownSections.Count), BigEndian);
                output.WriteByte(EncodingByte);
                output.Write(new byte[15]);

                WriteInf1(output);
                WriteDat1(output);
                WriteMid1(output);
                foreach (BmgSection section in UnknownSections)
                    WriteSection(output, section.Magic, section.Data, BigEndian, pad32: false);

                long end = output.Length;
                output.Position = 8;
                WriteUInt32(output, (uint)end, BigEndian);
                return output.ToArray();
            }

            //-------------------------------------------------------------------------------
            // INF1セクションを書き出す処理
            //-------------------------------------------------------------------------------
            private void WriteInf1(Stream output)
            {
                var data = new MemoryStream();
                WriteUInt16(data, (ushort)Messages.Count, BigEndian);
                WriteUInt16(data, (ushort)AttributeLength, BigEndian);
                WriteUInt32(data, 0, BigEndian);

                uint textOffset = 1;
                foreach (BmgMessage message in Messages)
                {
                    WriteUInt32(data, textOffset, BigEndian);
                    byte[] attributes = message.Attributes.Take(AttributeLength - 4).ToArray();
                    data.Write(attributes);
                    if (attributes.Length < AttributeLength - 4)
                        data.Write(new byte[AttributeLength - 4 - attributes.Length]);
                    textOffset += (uint)EncodeMessage(message.Text).Length;
                }

                WriteSection(output, "INF1", data.ToArray(), BigEndian, pad32: true);
            }

            //-------------------------------------------------------------------------------
            // DAT1セクションを書き出す処理
            //-------------------------------------------------------------------------------
            private void WriteDat1(Stream output)
            {
                var data = new MemoryStream();
                data.WriteByte(0);
                foreach (BmgMessage message in Messages)
                    data.Write(EncodeMessage(message.Text));

                WriteSection(output, "DAT1", data.ToArray(), BigEndian, pad32: true);
            }

            //-------------------------------------------------------------------------------
            // MID1セクションを書き出す処理
            //-------------------------------------------------------------------------------
            private void WriteMid1(Stream output)
            {
                var data = new MemoryStream();
                WriteUInt16(data, (ushort)Messages.Count, BigEndian);
                WriteUInt16(data, Mid1Value, BigEndian);
                WriteUInt32(data, 0, BigEndian);
                foreach (BmgMessage message in Messages)
                    WriteUInt32(data, (message.Id << 8) | message.SubId, BigEndian);

                WriteSection(output, "MID1", data.ToArray(), BigEndian, pad32: true);
            }

            //-------------------------------------------------------------------------------
            // BMG文字列をデコードする処理
            //-------------------------------------------------------------------------------
            private string DecodeMessage(byte[] datData, int offset)
            {
                var builder = new StringBuilder();
                var textBytes = new List<byte>();
                Encoding encoding = GetTextEncoding();
                int unitSize = EncodingByte == 2 ? 2 : 1;

                while (offset < datData.Length)
                {
                    ushort code = unitSize == 2 ? ReadUInt16(datData, offset, BigEndian) : datData[offset];
                    if (code == 0)
                        break;

                    if (code == 0x1A)
                    {
                        AppendDecodedText(builder, textBytes, encoding);
                        int lengthOffset = offset + unitSize;
                        int commandLength = datData[lengthOffset];
                        byte[] command = datData[offset..(offset + commandLength)];
                        builder.Append('{').Append(ToHex(command)).Append('}');
                        offset += commandLength;
                    }
                    else
                    {
                        for (int i = 0; i < unitSize; i++)
                            textBytes.Add(datData[offset + i]);
                        offset += unitSize;
                    }
                }

                AppendDecodedText(builder, textBytes, encoding);
                return builder.ToString();
            }

            //-------------------------------------------------------------------------------
            // BMG文字列をエンコードする処理
            //-------------------------------------------------------------------------------
            private byte[] EncodeMessage(string text)
            {
                var output = new MemoryStream();
                Encoding encoding = GetTextEncoding();

                for (int i = 0; i < text.Length;)
                {
                    if (text[i] == '\\' && i + 1 < text.Length && (text[i + 1] == '{' || text[i + 1] == '}' || text[i + 1] == '\\'))
                    {
                        output.Write(encoding.GetBytes(text.AsSpan(i + 1, 1).ToString()));
                        i += 2;
                    }
                    else if (text[i] == '\\')
                    {
                        output.Write(encoding.GetBytes("\\"));
                        i++;
                    }
                    else if (text[i] == '{')
                    {
                        int end = text.IndexOf('}', i + 1);
                        if (end < 0)
                            throw new InvalidDataException("Unclosed BMG escape sequence.");

                        output.Write(FromHex(text[(i + 1)..end]));
                        i = end + 1;
                    }
                    else
                    {
                        int next = NextSpecialIndex(text, i);
                        output.Write(encoding.GetBytes(text[i..next]));
                        i = next;
                    }
                }

                output.Write(EncodingByte == 2 ? new byte[2] : new byte[1]);
                return output.ToArray();
            }

            //-------------------------------------------------------------------------------
            // BMG本文の文字コードを取得する処理
            //-------------------------------------------------------------------------------
            private Encoding GetTextEncoding()
            {
                return EncodingByte switch
                {
                    0 => Encoding.Latin1,
                    1 => Encoding.Latin1,
                    2 => Encoding.BigEndianUnicode,
                    4 => Encoding.UTF8,
                    _ => Encoding.GetEncoding(932, EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback),
                };
            }
        }

        private sealed record BmgMessage(uint Id, byte SubId, byte[] Attributes, string Text);
        private sealed record BmgSection(string Magic, byte[] Data);

        //-------------------------------------------------------------------------------
        // 通常テキストを追加して作業バッファを空にする処理
        //-------------------------------------------------------------------------------
        private static void AppendDecodedText(StringBuilder builder, List<byte> textBytes, Encoding encoding)
        {
            if (textBytes.Count == 0)
                return;

            builder.Append(EscapePlainText(encoding.GetString(textBytes.ToArray())));
            textBytes.Clear();
        }

        //-------------------------------------------------------------------------------
        // BMG制御コードではない波括弧をエスケープする処理
        //-------------------------------------------------------------------------------
        private static string EscapePlainText(string text)
            => text.Replace("\\", "\\\\").Replace("{", "\\{").Replace("}", "\\}");

        //-------------------------------------------------------------------------------
        // エスケープ文字の次の位置を探す処理
        //-------------------------------------------------------------------------------
        private static int NextSpecialIndex(string text, int start)
        {
            for (int i = start; i < text.Length; i++)
            {
                if (text[i] == '{' || text[i] == '\\')
                    return i;
            }

            return text.Length;
        }

        //-------------------------------------------------------------------------------
        // セクション名を読み取る処理
        //-------------------------------------------------------------------------------
        private static string ReadMagic(byte[] data, int offset, bool bigEndian)
        {
            byte[] magic = data[offset..(offset + 4)];
            if (!bigEndian)
                Array.Reverse(magic);
            return Encoding.ASCII.GetString(magic);
        }

        //-------------------------------------------------------------------------------
        // UInt32を読み取る処理
        //-------------------------------------------------------------------------------
        private static uint ReadUInt32(byte[] data, int offset, bool bigEndian)
        {
            uint value = (uint)(data[offset] << 24 | data[offset + 1] << 16 | data[offset + 2] << 8 | data[offset + 3]);
            return bigEndian ? value : BinaryPrimitivesReverse(value);
        }

        //-------------------------------------------------------------------------------
        // UInt16を読み取る処理
        //-------------------------------------------------------------------------------
        private static ushort ReadUInt16(byte[] data, int offset, bool bigEndian)
        {
            ushort value = (ushort)(data[offset] << 8 | data[offset + 1]);
            return bigEndian ? value : (ushort)((value >> 8) | (value << 8));
        }

        //-------------------------------------------------------------------------------
        // UInt32を書き込む処理
        //-------------------------------------------------------------------------------
        private static void WriteUInt32(Stream stream, uint value, bool bigEndian)
        {
            if (!bigEndian)
                value = BinaryPrimitivesReverse(value);

            stream.WriteByte((byte)(value >> 24));
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)value);
        }

        //-------------------------------------------------------------------------------
        // UInt16を書き込む処理
        //-------------------------------------------------------------------------------
        private static void WriteUInt16(Stream stream, ushort value, bool bigEndian)
        {
            if (!bigEndian)
                value = (ushort)((value >> 8) | (value << 8));

            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)value);
        }

        //-------------------------------------------------------------------------------
        // BMGセクションを書き込む処理
        //-------------------------------------------------------------------------------
        private static void WriteSection(Stream stream, string magic, byte[] data, bool bigEndian, bool pad32)
        {
            byte[] magicBytes = Encoding.ASCII.GetBytes(magic);
            if (!bigEndian)
                Array.Reverse(magicBytes);
            stream.Write(magicBytes);
            uint size = (uint)(8 + data.Length);
            uint padding = pad32 ? (32 - (size % 32)) % 32 : 0;
            WriteUInt32(stream, size + padding, bigEndian);
            stream.Write(data);
            if (padding > 0)
                stream.Write(new byte[padding]);
        }

        //-------------------------------------------------------------------------------
        // UInt32のバイト順を反転する処理
        //-------------------------------------------------------------------------------
        private static uint BinaryPrimitivesReverse(uint value)
            => (value >> 24) | ((value >> 8) & 0x0000FF00) | ((value << 8) & 0x00FF0000) | (value << 24);
    }
}
