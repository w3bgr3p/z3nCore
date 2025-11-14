using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;


namespace z3nCore
{
    public static class AES
    {
        public static string EncryptAES(string phrase, string key, bool hashKey = true)
        {
            if (phrase == null || key == null)
                return null;

            var keyArray = HexStringToByteArray(hashKey ? HashMD5(key) : key);
            var toEncryptArray = Encoding.UTF8.GetBytes(phrase);
            byte[] result;

            using (var aes = new AesCryptoServiceProvider
            {
                Key = keyArray,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            })
            {
                var cTransform = aes.CreateEncryptor();
                result = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
                aes.Clear();
            }
            return ByteArrayToHexString(result);
        }
        public static string DecryptAES(string hash, string key, bool hashKey = true)
        {
            if (hash == null || key == null)
                return null;

            var keyArray = HexStringToByteArray(hashKey ? HashMD5(key) : key);
            var toEncryptArray = HexStringToByteArray(hash);

            var aes = new AesCryptoServiceProvider
            {
                Key = keyArray,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            var cTransform = aes.CreateDecryptor();
            var resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            aes.Clear();
            return Encoding.UTF8.GetString(resultArray);
        }
        private static string ByteArrayToHexString(byte[] inputArray)
        {
            if (inputArray == null)
                return null;
            var o = new StringBuilder("");
            for (var i = 0; i < inputArray.Length; i++)
                o.Append(inputArray[i].ToString("X2"));
            return o.ToString();
        }
        private static byte[] HexStringToByteArray(string inputString)
        {
            if (inputString == null)
                return null;

            if (inputString.Length == 0)
                return new byte[0];

            if (inputString.Length % 2 != 0)
                throw new Exception("Hex strings have an even number of characters and you have got an odd number of characters!");

            var num = inputString.Length / 2;
            var bytes = new byte[num];
            for (var i = 0; i < num; i++)
            {
                var x = inputString.Substring(i * 2, 2);
                try
                {
                    bytes[i] = Convert.ToByte(x, 16);
                }
                catch (Exception ex)
                {
                    throw new Exception("Part of your \"hex\" string contains a non-hex value.", ex);
                }
            }
            return bytes;
        }
        public static string HashMD5(string phrase)
        {
            if (phrase == null)
                return null;
            var encoder = new UTF8Encoding();
            var md5Hasher = new MD5CryptoServiceProvider();
            var hashedDataBytes = md5Hasher.ComputeHash(encoder.GetBytes(phrase));
            return ByteArrayToHexString(hashedDataBytes);
        }
    }
    public static class Bech32
    {
        private static readonly string Bech32Charset = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
        private static readonly uint[] Generator = { 0x3b6a57b2, 0x26508e6d, 0x1ea119fa, 0x3d4233dd, 0x2a1462b3 };

        
        
        
        public static string Encode(string prefix, byte[] data)
        {
            byte[] converted = ConvertBits(data, 8, 5, true);
            byte[] checksum = CreateChecksum(prefix, converted);

            StringBuilder result = new StringBuilder();
            result.Append(prefix);
            result.Append('1');
            foreach (byte b in converted.Concat(checksum))
                result.Append(Bech32Charset[b]);

            return result.ToString();
        }
        public static string Bech32ToHex(string bech32Address)
        {
            if (string.IsNullOrWhiteSpace(bech32Address))
                throw new ArgumentException("Bech32 address cannot be empty.");

            int sepIndex = bech32Address.IndexOf('1');
            if (sepIndex == -1)
                throw new ArgumentException("Invalid Bech32: separator '1' not found.");

            string hrp = bech32Address.Substring(0, sepIndex).ToLower();
            if (hrp != "init")
                throw new ArgumentException("Invalid Bech32 prefix. Expected 'init'.");

            string dataPart = bech32Address.Substring(sepIndex + 1);
            if (dataPart.Length < 6)
                throw new ArgumentException("Invalid Bech32: data too short.");

            byte[] data = dataPart.Select(c =>
            {
                int index = Bech32Charset.IndexOf(c);
                if (index == -1)
                    throw new ArgumentException($"Invalid Bech32 character: {c}");
                return (byte)index;
            }).ToArray();

            if (!VerifyChecksum(hrp, data))
                throw new ArgumentException("Invalid Bech32: checksum failed.");

            byte[] decoded = ConvertBits(data.Take(data.Length - 6).ToArray(), 5, 8, false);
            if (decoded.Length != 20)
                throw new ArgumentException("Invalid Bech32 data length. Expected 20 bytes.");

            return "0x" + BitConverter.ToString(decoded).Replace("-", "").ToLower();
        }

        public static string HexToBech32(string hexAddress, string prefix = "init")
        {
            if (string.IsNullOrWhiteSpace(hexAddress))
                throw new ArgumentException("HEX address cannot be empty.");

            if (hexAddress.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                hexAddress = hexAddress.Substring(2);

            if (hexAddress.Length != 40 || !IsHex(hexAddress))
                throw new ArgumentException("Invalid HEX address. Expected 40 hex characters.");

            byte[] data = Enumerable.Range(0, hexAddress.Length / 2)
                .Select(i => Convert.ToByte(hexAddress.Substring(i * 2, 2), 16))
                .ToArray();

            byte[] converted = ConvertBits(data, 8, 5, true);

            byte[] checksum = CreateChecksum(prefix, converted);

            StringBuilder result = new StringBuilder();
            result.Append(prefix);
            result.Append('1');
            foreach (byte b in converted.Concat(checksum))
                result.Append(Bech32Charset[b]);

            return result.ToString();
        }

        public static byte[] Bech32ToBytes(string bech32Address, string expectedPrefix)
        {
            if (string.IsNullOrWhiteSpace(bech32Address))
                throw new ArgumentException("Bech32 address cannot be empty.");

            int sepIndex = bech32Address.IndexOf('1');
            if (sepIndex == -1)
                throw new ArgumentException("Invalid Bech32: separator '1' not found.");

            string hrp = bech32Address.Substring(0, sepIndex).ToLower();
            if (hrp != expectedPrefix.ToLower())
                throw new ArgumentException($"Invalid Bech32 prefix. Expected '{expectedPrefix}'.");

            string dataPart = bech32Address.Substring(sepIndex + 1);
            if (dataPart.Length < 6)
                throw new ArgumentException("Invalid Bech32: data too short.");

            byte[] data = dataPart.Select(c =>
            {
                int index = Bech32Charset.IndexOf(c);
                if (index == -1)
                    throw new ArgumentException($"Invalid Bech32 character: {c}");
                return (byte)index;
            }).ToArray();

            if (!VerifyChecksum(hrp, data))
                throw new ArgumentException("Invalid Bech32: checksum failed.");

            // Убираем последние 6 байт (checksum) и конвертируем из 5-bit в 8-bit
            return ConvertBits(data.Take(data.Length - 6).ToArray(), 5, 8, false);
        }
        private static bool IsHex(string input)
        {
            return input.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
        }

        private static byte[] ConvertBits(byte[] data, int fromBits, int toBits, bool pad)
        {
            int acc = 0;
            int bits = 0;
            var result = new System.Collections.Generic.List<byte>();
            int maxv = (1 << toBits) - 1;

            foreach (byte value in data)
            {
                if (value < 0 || (value >> fromBits) != 0)
                    throw new ArgumentException("Invalid data for bit conversion.");

                acc = (acc << fromBits) | value;
                bits += fromBits;

                while (bits >= toBits)
                {
                    bits -= toBits;
                    result.Add((byte)((acc >> bits) & maxv));
                }
            }

            if (pad && bits > 0)
                result.Add((byte)((acc << (toBits - bits)) & maxv));
            else if (bits >= fromBits || ((acc << (toBits - bits)) & maxv) != 0)
                throw new ArgumentException("Invalid padding in bit conversion.");

            return result.ToArray();
        }

        private static bool VerifyChecksum(string hrp, byte[] data)
        {
            byte[] values = hrp.Expand().Concat(data).ToArray();
            return Polymod(values) == 1;
        }

        private static byte[] CreateChecksum(string hrp, byte[] data)
        {
            byte[] values = hrp.Expand().Concat(data).Concat(new byte[6]).ToArray();
            uint polymod = Polymod(values) ^ 1;
            var result = new byte[6];
            for (int i = 0; i < 6; i++)
                result[i] = (byte)((polymod >> (5 * (5 - i))) & 31);
            return result;
        }

        private static byte[] Expand(this string hrp)
        {
            var result = new byte[hrp.Length * 2 + 1];
            for (int i = 0; i < hrp.Length; i++)
            {
                result[i] = (byte)(hrp[i] >> 5);
                result[i + hrp.Length + 1] = (byte)(hrp[i] & 31);
            }
            return result;
        }

        private static uint Polymod(byte[] values)
        {
            uint chk = 1;
            foreach (byte value in values)
            {
                uint top = chk >> 25;
                chk = (chk & 0x1ffffff) << 5 ^ value;
                for (int i = 0; i < 5; i++)
                    if (((top >> i) & 1) != 0)
                        chk ^= Generator[i];
            }
            return chk;
        }

    }
    public static class Blake2b
    {
        private const ulong IV0 = 0x6A09E667F3BCC908UL;
        private const ulong IV1 = 0xBB67AE8584CAA73BUL;
        private const ulong IV2 = 0x3C6EF372FE94F82BUL;
        private const ulong IV3 = 0xA54FF53A5F1D36F1UL;
        private const ulong IV4 = 0x510E527FADE682D1UL;
        private const ulong IV5 = 0x9B05688C2B3E6C1FUL;
        private const ulong IV6 = 0x1F83D9ABFB41BD6BUL;
        private const ulong IV7 = 0x5BE0CD19137E2179UL;

        private static readonly byte[][] Sigma = new byte[][]
        {
            new byte[]{0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15},
            new byte[]{14,10,4,8,9,15,13,6,1,12,0,2,11,7,5,3},
            new byte[]{11,8,12,0,5,2,15,13,10,14,3,6,7,1,9,4},
            new byte[]{7,9,3,1,13,12,11,14,2,6,5,10,4,0,15,8},
            new byte[]{9,0,5,7,2,4,10,15,14,1,11,12,6,8,3,13},
            new byte[]{2,12,6,10,0,11,8,3,4,13,7,5,15,14,1,9},
            new byte[]{12,5,1,15,14,13,4,10,0,7,6,3,9,2,8,11},
            new byte[]{13,11,7,14,12,1,3,9,5,0,15,4,8,6,2,10},
            new byte[]{6,15,14,9,11,3,0,8,12,2,13,7,1,4,10,5},
            new byte[]{10,2,8,4,7,6,1,5,15,11,9,14,3,12,13,0},
            new byte[]{0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15},
            new byte[]{14,10,4,8,9,15,13,6,1,12,0,2,11,7,5,3}
        };

        public static byte[] ComputeHash(byte[] input, int outLen = 32)
        {
            // Инициализация h[0] с параметрами
            ulong h0 = IV0 ^ 0x01010000UL ^ (ulong)outLen;
            ulong[] h = new ulong[] { h0, IV1, IV2, IV3, IV4, IV5, IV6, IV7 };
            
            ulong t = 0;
            int offset = 0;
            byte[] block = new byte[128];

            // Обработка полных блоков
            while (input.Length - offset > 128)
            {
                Array.Copy(input, offset, block, 0, 128);
                t += 128;
                Compress(h, block, t, false);
                offset += 128;
            }

            // Последний блок
            int last = input.Length - offset;
            Array.Clear(block, 0, 128);
            Array.Copy(input, offset, block, 0, last);
            t += (ulong)last;
            Compress(h, block, t, true);

            // Извлечение результата в little-endian
            byte[] result = new byte[outLen];
            for (int i = 0; i < outLen; i++)
            {
                result[i] = (byte)(h[i / 8] >> (8 * (i % 8)));
            }

            return result;
        }

        private static void Compress(ulong[] h, byte[] block, ulong t, bool last)
        {
            ulong[] v = new ulong[16];
            ulong[] m = new ulong[16];
            
            Array.Copy(h, 0, v, 0, 8);
            v[8] = IV0;
            v[9] = IV1;
            v[10] = IV2;
            v[11] = IV3;
            v[12] = IV4 ^ t;
            v[13] = IV5;
            v[14] = last ? ~IV6 : IV6;
            v[15] = IV7;

            // Читаем блок как little-endian uint64
            for (int i = 0; i < 16; i++)
            {
                m[i] = BitConverter.ToUInt64(block, i * 8);
            }

            // 12 раундов
            for (int r = 0; r < 12; r++)
            {
                byte[] s = Sigma[r];
                G(ref v[0], ref v[4], ref v[8], ref v[12], m[s[0]], m[s[1]]);
                G(ref v[1], ref v[5], ref v[9], ref v[13], m[s[2]], m[s[3]]);
                G(ref v[2], ref v[6], ref v[10], ref v[14], m[s[4]], m[s[5]]);
                G(ref v[3], ref v[7], ref v[11], ref v[15], m[s[6]], m[s[7]]);
                G(ref v[0], ref v[5], ref v[10], ref v[15], m[s[8]], m[s[9]]);
                G(ref v[1], ref v[6], ref v[11], ref v[12], m[s[10]], m[s[11]]);
                G(ref v[2], ref v[7], ref v[8], ref v[13], m[s[12]], m[s[13]]);
                G(ref v[3], ref v[4], ref v[9], ref v[14], m[s[14]], m[s[15]]);
            }

            for (int i = 0; i < 8; i++)
            {
                h[i] ^= v[i] ^ v[i + 8];
            }
        }

        private static void G(ref ulong a, ref ulong b, ref ulong c, ref ulong d, ulong x, ulong y)
        {
            a = a + b + x;
            d = RotRight(d ^ a, 32);
            c = c + d;
            b = RotRight(b ^ c, 24);
            a = a + b + y;
            d = RotRight(d ^ a, 16);
            c = c + d;
            b = RotRight(b ^ c, 63);
        }

        private static ulong RotRight(ulong x, int n)
        {
            return (x >> n) | (x << (64 - n));
        }
    }


}
