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


}
