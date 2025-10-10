using System;
using System.Collections.Generic;
using System.Linq;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore.Utilities
{
    public static class Converer
    {
        
        public static string ConvertFormat(IZennoPosterProjectModel project, string toProcess, string input, string output, bool log = false)
        {
            try
            {
                input = input.ToLower();
                output = output.ToLower();

                string[] supportedFormats = { "hex", "base64", "bech32", "bytes", "text" };
                if (!supportedFormats.Contains(input))
                {
                    throw new ArgumentException($"Неподдерживаемый входной формат: {input}. Поддерживаемые форматы: {string.Join(", ", supportedFormats)}");
                }
                if (!supportedFormats.Contains(output))
                {
                    throw new ArgumentException($"Неподдерживаемый выходной формат: {output}. Поддерживаемые форматы: {string.Join(", ", supportedFormats)}");
                }

                byte[] bytes;
                switch (input)
                {
                    case "hex":
                        string hex = toProcess.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? toProcess.Substring(2) : toProcess;
                        hex = hex.PadLeft(64, '0');
                        if (!System.Text.RegularExpressions.Regex.IsMatch(hex, @"^[0-9a-fA-F]+$"))
                        {
                            throw new ArgumentException("Входная строка не является валидной hex-строкой");
                        }
                        bytes = new byte[hex.Length / 2];
                        for (int i = 0; i < hex.Length; i += 2)
                        {
                            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                        }
                        break;

                    case "base64":
                        bytes = Convert.FromBase64String(toProcess);
                        break;

                    case "bech32":
                        var (hrp, data) = DecodeBech32(toProcess);
                        if (hrp != "init")
                        {
                            throw new ArgumentException($"Ожидался Bech32-адрес с префиксом 'init', но получен префикс '{hrp}'");
                        }
                        bytes = ConvertBits(data, 5, 8, false);
                        if (bytes.Length != 32)
                        {
                            throw new ArgumentException($"Bech32-адрес должен декодироваться в 32 байта, но получено {bytes.Length} байт");
                        }
                        break;

                    case "bytes":
                        if (!System.Text.RegularExpressions.Regex.IsMatch(toProcess, @"^[0-9a-fA-F]+$"))
                        {
                            throw new ArgumentException("Входная строка не является валидной hex-строкой для байтов");
                        }
                        bytes = new byte[toProcess.Length / 2];
                        for (int i = 0; i < toProcess.Length; i += 2)
                        {
                            bytes[i / 2] = Convert.ToByte(toProcess.Substring(i, 2), 16);
                        }
                        break;

                    case "text":
                        bytes = System.Text.Encoding.UTF8.GetBytes(toProcess);
                        break;

                    default:
                        throw new ArgumentException($"Неизвестный входной формат: {input}");
                }

                string result;
                switch (output)
                {
                    case "hex":
                        result = "0x" + BitConverter.ToString(bytes).Replace("-", "").ToLower();
                        break;

                    case "base64":
                        result = Convert.ToBase64String(bytes);
                        break;

                    case "bech32":
                        if (bytes.Length != 32)
                        {
                            throw new ArgumentException($"Для Bech32 требуется 32 байта, но получено {bytes.Length} байт");
                        }
                        byte[] data5Bit = ConvertBits(bytes, 8, 5, true);
                        result = EncodeBech32("init", data5Bit);
                        break;

                    case "bytes":
                        result = BitConverter.ToString(bytes).Replace("-", "").ToLower();
                        break;

                    case "text":
                        result = System.Text.Encoding.UTF8.GetString(bytes);
                        break;

                    default:
                        throw new ArgumentException($"Неизвестный выходной формат: {output}");
                }

                if (log) project.SendInfoToLog($"convert success: {toProcess} ({input}) -> {result} ({output})");
                return result;
            }
            catch (Exception ex)
            {
                project.SendErrorToLog($"Ошибка при преобразовании: {ex.Message}");
                return null;
            }
        }
        private static (string hrp, byte[] data) DecodeBech32(string bech32)
        {
            if (string.IsNullOrEmpty(bech32) || bech32.Length > 1023)
            {
                throw new ArgumentException("Невалидная Bech32-строка");
            }

            int separatorIndex = bech32.LastIndexOf('1');
            if (separatorIndex < 1 || separatorIndex + 7 > bech32.Length)
            {
                throw new ArgumentException("Невалидный формат Bech32: отсутствует разделитель '1'");
            }

            string hrp = bech32.Substring(0, separatorIndex);
            string dataPart = bech32.Substring(separatorIndex + 1);

            if (!VerifyChecksum(hrp, dataPart))
            {
                throw new ArgumentException("Невалидная контрольная сумма Bech32");
            }

            byte[] data = new byte[dataPart.Length - 6]; // Убираем 6 байт контрольной суммы
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)Bech32Charset.IndexOf(dataPart[i]);
                if (data[i] == 255)
                {
                    throw new ArgumentException($"Невалидный символ в Bech32: {dataPart[i]}");
                }
            }

            return (hrp, data);
        }
        private static string EncodeBech32(string hrp, byte[] data)
        {
            string checksum = CreateChecksum(hrp, data);
            string combined = string.Concat(data.Select(b => Bech32Charset[b])) + checksum;
            return hrp + "1" + combined;
        }
        private static byte[] ConvertBits(byte[] data, int fromBits, int toBits, bool pad)
        {
            int acc = 0;
            int bits = 0;
            var result = new List<byte>();
            int maxv = (1 << toBits) - 1;
            int maxAcc = (1 << (fromBits + toBits - 1)) - 1;

            foreach (var value in data)
            {
                acc = ((acc << fromBits) | value) & maxAcc;
                bits += fromBits;
                while (bits >= toBits)
                {
                    bits -= toBits;
                    result.Add((byte)((acc >> bits) & maxv));
                }
            }

            if (pad && bits > 0)
            {
                result.Add((byte)((acc << (toBits - bits)) & maxv));
            }
            else if (bits >= fromBits || ((acc << (toBits - bits)) & maxv) != 0)
            {
                throw new ArgumentException("Невозможно преобразовать биты без потерь");
            }

            return result.ToArray();
        }
        private static readonly string Bech32Charset = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
        private static bool VerifyChecksum(string hrp, string data)
        {
            var values = new List<byte>();
            foreach (char c in hrp.ToLower())
            {
                values.Add((byte)c);
            }
            values.Add(0);
            foreach (char c in data)
            {
                int v = Bech32Charset.IndexOf(c);
                if (v == -1) return false;
                values.Add((byte)v);
            }
            return Polymod(values) == 1;
        }
        private static string CreateChecksum(string hrp, byte[] data)
        {
            var values = new List<byte>();
            foreach (char c in hrp.ToLower())
            {
                values.Add((byte)c);
            }
            values.Add(0);
            values.AddRange(data);
            values.AddRange(new byte[] { 0, 0, 0, 0, 0, 0 }); // 6 байт для контрольной суммы
            int polymod = Polymod(values) ^ 1;
            var checksum = new char[6];
            for (int i = 0; i < 6; i++)
            {
                checksum[i] = Bech32Charset[(polymod >> (5 * (5 - i))) & 31];
            }
            return new string(checksum);
        }
        private static int Polymod(List<byte> values)
        {
            int chk = 1;
            int[] generator = { 0x3b6a57b2, 0x26508e6d, 0x1ea119fa, 0x3d4233dd, 0x2a1462b3 };
            foreach (byte value in values)
            {
                int b = chk >> 25;
                chk = (chk & 0x1ffffff) << 5 ^ value;
                for (int i = 0; i < 5; i++)
                {
                    if (((b >> i) & 1) != 0)
                    {
                        chk ^= generator[i];
                    }
                }
            }
            return chk;
        }
    }

}
