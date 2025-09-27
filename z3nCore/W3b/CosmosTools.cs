using System;
using System.Linq;
using System.Security.Cryptography;
using NBitcoin;
using NBitcoin.Crypto;

namespace z3nCore.W3b
{
    /// <summary>
    /// Инструменты для работы с Cosmos SDK кошельками
    /// </summary>
    public class CosmosTools
    {
        #region Public API

        /// <summary>
        /// Получает приватный ключ из мнемонической фразы
        /// </summary>
        /// <param name="mnemonic">Мнемоническая фраза (12-24 слова)</param>
        /// <returns>Приватный ключ в hex формате</returns>
        public string KeyFromSeed(string mnemonic)
        {
            var derivedKey = GetDerivedKeyFromMnemonic(mnemonic);
            byte[] privateKey = derivedKey.PrivateKey.ToBytes();
            return BitConverter.ToString(privateKey).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Получает адрес кошелька из мнемонической фразы
        /// </summary>
        /// <param name="mnemonic">Мнемоническая фраза</param>
        /// <param name="chain">Префикс сети (по умолчанию "cosmos")</param>
        /// <returns>Bech32 адрес кошелька</returns>
        public string AddressFromSeed(string mnemonic, string chain = "cosmos")
        {
            var derivedKey = GetDerivedKeyFromMnemonic(mnemonic);
            return GetAddressFromExtKey(derivedKey, chain);
        }

        /// <summary>
        /// Получает адрес кошелька из приватного ключа
        /// </summary>
        /// <param name="privateKey">Приватный ключ в hex формате</param>
        /// <param name="chain">Префикс сети (по умолчанию "cosmos")</param>
        /// <returns>Bech32 адрес кошелька</returns>
        public string AddressFromKey(string privateKey, string chain = "cosmos")
        {
            byte[] privateKeyBytes = StringToByteArray(privateKey);
            var pubKey = new PubKey(new Key(privateKeyBytes).PubKey.ToBytes());
            byte[] publicKey = pubKey.ToBytes();

            byte[] ripemd160Hash = GetAddressHash(publicKey);
            return EncodeBech32(chain, ripemd160Hash);
        }

        /// <summary>
        /// Получает приватный ключ и адрес из мнемонической фразы
        /// </summary>
        /// <param name="mnemonic">Мнемоническая фраза</param>
        /// <param name="chain">Префикс сети (по умолчанию "cosmos")</param>
        /// <returns>Массив [privateKey, address]</returns>
        public string[] AccFromSeed(string mnemonic, string chain = "cosmos")
        {
            var derivedKey = GetDerivedKeyFromMnemonic(mnemonic);
            
            // Приватный ключ
            byte[] privateKey = derivedKey.PrivateKey.ToBytes();
            string privateKeyHex = BitConverter.ToString(privateKey).Replace("-", "").ToLowerInvariant();
            
            // Адрес
            string address = GetAddressFromExtKey(derivedKey, chain);

            return new string[] { privateKeyHex, address };
        }

        #endregion

        #region Private Implementation

        /// <summary>
        /// Получает производный ключ из мнемонической фразы по стандартному пути Cosmos
        /// </summary>
        private ExtKey GetDerivedKeyFromMnemonic(string mnemonic)
        {
            var mnemo = new Mnemonic(mnemonic, Wordlist.English);
            byte[] seed = mnemo.DeriveSeed();
            var masterKey = ExtKey.CreateFromSeed(seed);
            
            // Стандартный путь для Cosmos SDK: m/44'/118'/0'/0/0
            var path = new KeyPath("m/44'/118'/0'/0/0");
            return masterKey.Derive(path);
        }

        /// <summary>
        /// Получает адрес из производного ключа
        /// </summary>
        private string GetAddressFromExtKey(ExtKey derivedKey, string chain)
        {
            byte[] publicKey = new PubKey(derivedKey.PrivateKey.PubKey.ToBytes()).ToBytes();
            byte[] ripemd160Hash = GetAddressHash(publicKey);
            return EncodeBech32(chain, ripemd160Hash);
        }

        /// <summary>
        /// Получает хеш адреса из публичного ключа (SHA256 -> RIPEMD160)
        /// </summary>
        private byte[] GetAddressHash(byte[] publicKey)
        {
            byte[] sha256Hash;
            using (var sha256 = SHA256.Create())
            {
                sha256Hash = sha256.ComputeHash(publicKey);
            }
            return Hashes.RIPEMD160(sha256Hash);
        }

        #endregion

        #region Crypto Utilities

        /// <summary>
        /// Конвертирует hex строку в массив байт
        /// </summary>
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

        /// <summary>
        /// Конвертирует биты для Bech32 кодирования
        /// </summary>
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
                throw new InvalidOperationException("Invalid padding");
            }

            return result.ToArray();
        }

        /// <summary>
        /// Создает расширенный HRP для Bech32
        /// </summary>
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

        /// <summary>
        /// Вычисляет Bech32 полином для контрольной суммы
        /// </summary>
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

        /// <summary>
        /// Кодирует данные в Bech32 формат
        /// </summary>
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

        #endregion
    }
}