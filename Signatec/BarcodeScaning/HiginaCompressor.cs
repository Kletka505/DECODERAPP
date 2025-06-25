using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Lomont.Compression;

namespace Signatec.BarcodeScaning
{
    /// <summary>
    /// Декомпрессор штрихкодов Хижины
    /// </summary>
    public static class HiginaCompressor
    {
        private static readonly string[] SplitStrings = { "<^>", "|", "¦" };
        private static readonly Regex[] CheckPatterns = { new Regex("#.+\\<\\^\\>"), new Regex("#.+\\|"), new Regex("#.+\\¦") };

        /// <summary>
        /// Распаковать штрихкод хижины
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="isOnlyCheck">Признак того что нужно только проверить формат</param>
        /// <returns></returns>
        public static Barcode TryDecompress(byte[] buffer, bool isOnlyCheck = false)
        {
            try
            {
                var comp = new ArithmeticCompressor();
                var res = comp.Decompress(buffer);
                var str = Encoding.GetEncoding(1251).GetString(res);
                if (str.Contains("\0"))
                {
                    return null;
                }

                var index = Array.FindIndex(CheckPatterns, x => x.IsMatch(str));
                if (isOnlyCheck)
                {
                    return index >= 0 ? new Barcode { SourceType = BarcodeSourceType.Higina } : null;
                }
                var parts = str.Split(new[] { SplitStrings[index] }, StringSplitOptions.None).ToList();
                if (2 > parts.Count)
                {
                    return null;
                }
                if (parts[0].StartsWith("<") && parts[0].EndsWith(">"))
                {
                    parts.RemoveAt(0);
                }

                var fields = new List<BarcodeDocField>();
                for (var i = 0; i < parts.Count - 1; i += 2)
                {
                    if (string.IsNullOrEmpty(parts[i]) || ('#' != parts[i][0]))
                    {
                        return null;
                    }
                    fields.Add(new BarcodeDocField { Name = parts[i], Value = parts[i + 1] });
                }
                return new Barcode { Fields = fields.ToArray(), SourceType = BarcodeSourceType.Higina };
            }
            catch
            {
                return null;
            }
        }
    }
}