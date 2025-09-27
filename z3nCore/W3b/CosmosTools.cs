


namespace z3nCore.W3b
{
    using System;
    using System.Linq;
    using NBitcoin.Crypto;
    using System.Threading.Tasks;
    public class CosmosTools
    {
        public async Task<string> KeyFromSeed(string mnemonic)
        {
            NBitcoin.Mnemonic mnemo = new NBitcoin.Mnemonic(mnemonic, NBitcoin.Wordlist.English);
            byte[] seed = mnemo.DeriveSeed();
            NBitcoin.ExtKey masterKey = NBitcoin.ExtKey.CreateFromSeed(seed);
            NBitcoin.KeyPath path = new NBitcoin.KeyPath("m/44'/118'/0'/0/0");
            NBitcoin.ExtKey derivedKey = masterKey.Derive(path);
            byte[] privateKey = derivedKey.PrivateKey.ToBytes();
            return BitConverter.ToString(privateKey).Replace("-", "").ToLowerInvariant();
        }

        public async Task<string> AddressFromSeed(string mnemonic, string chain = "cosmos")
        {
            NBitcoin.Mnemonic mnemo = new NBitcoin.Mnemonic(mnemonic, NBitcoin.Wordlist.English);
            byte[] seed = mnemo.DeriveSeed();
            NBitcoin.ExtKey masterKey = NBitcoin.ExtKey.CreateFromSeed(seed);
            NBitcoin.KeyPath path = new NBitcoin.KeyPath("m/44'/118'/0'/0/0");
            NBitcoin.ExtKey derivedKey = masterKey.Derive(path);
            byte[] privateKey = derivedKey.PrivateKey.ToBytes();
            byte[] publicKey = new NBitcoin.PubKey(derivedKey.PrivateKey.PubKey.ToBytes()).ToBytes();

            byte[] sha256Hash;
            using (System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create())
            {
                sha256Hash = sha256.ComputeHash(publicKey);
            }

            byte[] ripemd160Hash = Ripemd160(sha256Hash);

            string address = EncodeBech32(chain, ripemd160Hash);
            return address;
        }

        public async Task<string> AddressFromKey(string privateKey, string chain = "cosmos")
        {
            byte[] privateKeyBytes = StringToByteArray(privateKey);
            NBitcoin.PubKey pubKey = new NBitcoin.PubKey(new NBitcoin.Key(privateKeyBytes).PubKey.ToBytes());
            byte[] publicKey = pubKey.ToBytes();

            byte[] sha256Hash;
            using (System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create())
            {
                sha256Hash = sha256.ComputeHash(publicKey);
            }

            byte[] ripemd160Hash = Ripemd160(sha256Hash);

            string address = EncodeBech32(chain, ripemd160Hash);
            return address;
        }

        public async Task<string[]> AccFromSeed(string mnemonic, string chain = "cosmos")
        {
            NBitcoin.Mnemonic mnemo = new NBitcoin.Mnemonic(mnemonic, NBitcoin.Wordlist.English);
            byte[] seed = mnemo.DeriveSeed();
            NBitcoin.ExtKey masterKey = NBitcoin.ExtKey.CreateFromSeed(seed);
            NBitcoin.KeyPath path = new NBitcoin.KeyPath("m/44'/118'/0'/0/0");
            NBitcoin.ExtKey derivedKey = masterKey.Derive(path);
            byte[] privateKey = derivedKey.PrivateKey.ToBytes();
            string privateKeyHex = BitConverter.ToString(privateKey).Replace("-", "").ToLowerInvariant();

            byte[] publicKey = new NBitcoin.PubKey(derivedKey.PrivateKey.PubKey.ToBytes()).ToBytes();
            byte[] sha256Hash;
            using (System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create())
            {
                sha256Hash = sha256.ComputeHash(publicKey);
            }

            byte[] ripemd160Hash = Ripemd160(sha256Hash);
            string address = EncodeBech32(chain, ripemd160Hash);

            return new string[] { privateKeyHex, address };
        }

        private static byte[] Ripemd160(byte[] data)
        {
            return Hashes.RIPEMD160(data);
        }

        private static byte[] StringToByteArray(string hex)
        {
            int numberChars = hex.Length;
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }

        private static byte[] ConvertBits(byte[] data, int fromBits, int toBits, bool pad)
        {
            int acc = 0;
            int bits = 0;
            var result = new System.Collections.Generic.List<byte>();
            int maxv = (1 << toBits) - 1;

            foreach (byte value in data)
            {
                acc = (acc << fromBits) | value;
                bits += fromBits;
                while (bits >= toBits)
                {
                    bits -= toBits;
                    result.Add((byte)((acc >> bits) & maxv));
                }
            }

            if (pad)
            {
                if (bits > 0)
                {
                    result.Add((byte)((acc << (toBits - bits)) & maxv));
                }
            }
            else if (bits >= fromBits || ((acc << (toBits - bits)) & maxv) != 0)
            {
                throw new System.InvalidOperationException("Invalid padding");
            }

            return result.ToArray();
        }

        private static byte[] CreateHrpExpanded(string hrp)
        {
            byte[] hrpBytes = System.Text.Encoding.ASCII.GetBytes(hrp.ToLowerInvariant());
            byte[] expanded = new byte[hrpBytes.Length * 2 + 1];
            for (int i = 0; i < hrpBytes.Length; i++)
            {
                expanded[i] = (byte)(hrpBytes[i] >> 5);
                expanded[i + hrpBytes.Length + 1] = (byte)(hrpBytes[i] & 0x1f);
            }

            expanded[hrpBytes.Length] = 0;
            return expanded;
        }

        private static uint Bech32Polymod(byte[] values)
        {
            uint[] GENERATOR = { 0x3b6a57b2, 0x26508e6d, 0x1ea119fa, 0x3d4233dd, 0x2a1462b3 };
            uint chk = 1;
            foreach (byte v in values)
            {
                uint top = chk >> 25;
                chk = (chk & 0x1ffffff) << 5 ^ v;
                for (int i = 0; i < 5; i++)
                {
                    if ((top >> i & 1) != 0)
                        chk ^= GENERATOR[i];
                }
            }

            return chk ^ 1;
        }

        private static string EncodeBech32(string prefix, byte[] data)
        {
            const string charset = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
            byte[] dataWithHrp = ConvertBits(data, 8, 5, true);
            byte[] hrpExpanded = CreateHrpExpanded(prefix);
            byte[] values = hrpExpanded.Concat(dataWithHrp).Concat(new byte[6]).ToArray();
            uint checksum = Bech32Polymod(values);
            byte[] checksumBytes = new byte[6];
            for (int i = 0; i < 6; i++)
            {
                checksumBytes[i] = (byte)((checksum >> (5 * (5 - i))) & 0x1f);
            }

            string dataPart = new string(dataWithHrp.Select(b => charset[b]).ToArray());
            string checksumPart = new string(checksumBytes.Select(b => charset[b]).ToArray());
            return prefix + "1" + dataPart + checksumPart;
        }
    }
}