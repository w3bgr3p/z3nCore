using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Numerics;
using System.Threading.Tasks;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore.W3b
{
    /// <summary>
    /// Класс для работы с EVM RPC
    /// Использует project.POST() для управления соединениями (поддержка прокси и useNetHttp режима)
    /// </summary>
    public class EvmTools
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly bool _useNetHttp;
        private readonly bool _log;

        public EvmTools(IZennoPosterProjectModel project, bool useNetHttp = false, bool log = false)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _useNetHttp = useNetHttp;
            _log = log;
        }

        /// <summary>
        /// Ожидает выполнения транзакции с расширенной информацией о pending статусе
        /// </summary>
        public async Task<bool> WaitTxExtended(string rpc, string hash, int deadline = 60, string proxy = "")
        {
            string jsonReceipt = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_getTransactionReceipt"", ""params"": [""{hash}""], ""id"": 1 }}";
            string jsonRaw = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_getTransactionByHash"", ""params"": [""{hash}""], ""id"": 1 }}";

            var startTime = DateTime.Now;
            var timeout = TimeSpan.FromSeconds(deadline);

            while (true)
            {
                if (DateTime.Now - startTime > timeout)
                    throw new Exception($"Timeout {deadline}s");

                try
                {
                    string body = await Task.Run(() =>
                        _project.POST(rpc, jsonReceipt, proxy, useNetHttp: _useNetHttp, log: false, thrw: false)
                    );

                    if (body.StartsWith("Error:") || body.Contains("!!!"))
                    {
                        if (_log) Console.WriteLine($"Server error (receipt): {body}");
                        await Task.Delay(2000);
                        continue;
                    }

                    var json = JObject.Parse(body);

                    if (json["result"] == null || json["result"].Type == JTokenType.Null)
                    {
                        body = await Task.Run(() =>
                            _project.POST(rpc, jsonRaw, proxy, useNetHttp: _useNetHttp, log: false, thrw: false)
                        );

                        if (body.StartsWith("Error:") || body.Contains("!!!"))
                        {
                            if (_log) Console.WriteLine($"Server error (raw): {body}");
                            await Task.Delay(2000);
                            continue;
                        }

                        var rawJson = JObject.Parse(body);

                        if (rawJson["result"] == null || rawJson["result"].Type == JTokenType.Null)
                        {
                            if (_log) Console.WriteLine($"[{rpc} {hash}] not found");
                        }
                        else
                        {
                            if (_log)
                            {
                                string gas = (rawJson["result"]?["maxFeePerGas"]?.ToString() ?? "0").Replace("0x", "");
                                string gasPrice = (rawJson["result"]?["gasPrice"]?.ToString() ?? "0").Replace("0x", "");
                                string nonce = (rawJson["result"]?["nonce"]?.ToString() ?? "0").Replace("0x", "");
                                string value = (rawJson["result"]?["value"]?.ToString() ?? "0").Replace("0x", "");
                                Console.WriteLine($"[{rpc} {hash}] pending  gasLimit:[{BigInteger.Parse(gas, NumberStyles.AllowHexSpecifier)}] gasNow:[{BigInteger.Parse(gasPrice, NumberStyles.AllowHexSpecifier)}] nonce:[{BigInteger.Parse(nonce, NumberStyles.AllowHexSpecifier)}] value:[{BigInteger.Parse(value, NumberStyles.AllowHexSpecifier)}]");
                            }
                        }
                    }
                    else
                    {
                        string status = json["result"]?["status"]?.ToString().Replace("0x", "") ?? "0";
                        string gasUsed = json["result"]?["gasUsed"]?.ToString().Replace("0x", "") ?? "0";
                        string gasPrice = json["result"]?["effectiveGasPrice"]?.ToString().Replace("0x", "") ?? "0";

                        bool success = status == "1";
                        if (_log)
                        {
                            Console.WriteLine($"[{rpc} {hash}] {(success ? "SUCCESS" : "FAIL")} gasUsed: {BigInteger.Parse(gasUsed, NumberStyles.AllowHexSpecifier)}");
                        }
                        return success;
                    }
                }
                catch (Exception ex)
                {
                    if (_log) Console.WriteLine($"Request error: {ex.Message}");
                    await Task.Delay(2000);
                    continue;
                }

                await Task.Delay(3000);
            }
        }

        /// <summary>
        /// Ожидает выполнения транзакции (упрощенная версия)
        /// </summary>
        public async Task<bool> WaitTx(string rpc, string hash, int deadline = 60, string proxy = "")
        {
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_getTransactionReceipt"", ""params"": [""{hash}""], ""id"": 1 }}";

            var startTime = DateTime.Now;
            var timeout = TimeSpan.FromSeconds(deadline);

            while (true)
            {
                if (DateTime.Now - startTime > timeout)
                    throw new Exception($"Timeout {deadline}s");

                try
                {
                    string body = await Task.Run(() =>
                        _project.POST(rpc, jsonBody, proxy, useNetHttp: _useNetHttp, log: false, thrw: false)
                    );

                    if (body.StartsWith("Error:") || body.Contains("!!!"))
                    {
                        if (_log) Console.WriteLine($"Server error: {body}");
                        await Task.Delay(2000);
                        continue;
                    }

                    var json = JObject.Parse(body);

                    if (json["result"] == null || json["result"].Type == JTokenType.Null)
                    {
                        if (_log) Console.WriteLine($"[{rpc} {hash}] not found");
                        await Task.Delay(2000);
                        continue;
                    }

                    string status = json["result"]?["status"]?.ToString().Replace("0x", "") ?? "0";
                    bool success = status == "1";
                    if (_log) Console.WriteLine($"[{rpc} {hash}] {(success ? "SUCCESS" : "FAIL")}");
                    return success;
                }
                catch (Exception ex)
                {
                    if (_log) Console.WriteLine($"Request error: {ex.Message}");
                    await Task.Delay(2000);
                    continue;
                }
            }
        }

        /// <summary>
        /// Получает баланс нативной монеты
        /// </summary>
        public async Task<string> Native(string rpc, string address, string proxy = "")
        {
            address = address.NormalizeAddress();
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_getBalance"", ""params"": [""{address}"", ""latest""], ""id"": 1 }}";

            string body = await Task.Run(() =>
                _project.POST(rpc, jsonBody, proxy, useNetHttp: _useNetHttp, log: _log, thrw: true)
            );

            var json = JObject.Parse(body);
            string hexBalance = json["result"]?.ToString().Replace("0x", "") ?? "0";
            return hexBalance;
        }

        /// <summary>
        /// Получает баланс ERC20 токена
        /// </summary>
        public async Task<string> Erc20(string tokenContract, string rpc, string address, string proxy = "")
        {
            tokenContract = tokenContract.NormalizeAddress();
            address = address.NormalizeAddress();
            string data = "0x70a08231000000000000000000000000" + address.Replace("0x", "");
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_call"", ""params"": [{{ ""to"": ""{tokenContract}"", ""data"": ""{data}"" }}, ""latest""], ""id"": 1 }}";

            string body = await Task.Run(() =>
                _project.POST(rpc, jsonBody, proxy, useNetHttp: _useNetHttp, log: _log, thrw: true)
            );

            var json = JObject.Parse(body);
            string hexBalance = json["result"]?.ToString().Replace("0x", "") ?? "0";
            return hexBalance;
        }

        /// <summary>
        /// Получает баланс ERC721 NFT
        /// </summary>
        public async Task<string> Erc721(string tokenContract, string rpc, string address, string proxy = "")
        {
            tokenContract = tokenContract.NormalizeAddress();
            address = address.NormalizeAddress();
            string data = "0x70a08231000000000000000000000000" + address.Replace("0x", "").ToLower();
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_call"", ""params"": [{{ ""to"": ""{tokenContract}"", ""data"": ""{data}"" }}, ""latest""], ""id"": 1 }}";

            string body = await Task.Run(() =>
                _project.POST(rpc, jsonBody, proxy, useNetHttp: _useNetHttp, log: _log, thrw: true)
            );

            var json = JObject.Parse(body);
            string hexBalance = json["result"]?.ToString().Replace("0x", "") ?? "0";
            return hexBalance;
        }

        /// <summary>
        /// Получает баланс ERC1155 токена
        /// </summary>
        public async Task<string> Erc1155(string tokenContract, string tokenId, string rpc, string address, string proxy = "")
        {
            tokenContract = tokenContract.NormalizeAddress();
            address = address.NormalizeAddress();
            string data = "0x00fdd58e" + address.Replace("0x", "").ToLower().PadLeft(64, '0') + BigInteger.Parse(tokenId).ToString("x").PadLeft(64, '0');
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_call"", ""params"": [{{ ""to"": ""{tokenContract}"", ""data"": ""{data}"" }}, ""latest""], ""id"": 1 }}";

            string body = await Task.Run(() =>
                _project.POST(rpc, jsonBody, proxy, useNetHttp: _useNetHttp, log: _log, thrw: true)
            );

            var json = JObject.Parse(body);
            string hexBalance = json["result"]?.ToString().Replace("0x", "") ?? "0";
            return hexBalance;
        }

        /// <summary>
        /// Получает nonce (количество транзакций) для адреса
        /// </summary>
        public async Task<string> Nonce(string rpc, string address, string proxy = "")
        {
            address = address.NormalizeAddress();
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_getTransactionCount"", ""params"": [""{address}"", ""latest""], ""id"": 1 }}";

            try
            {
                string body = await Task.Run(() =>
                    _project.POST(rpc, jsonBody, proxy, useNetHttp: _useNetHttp, log: _log, thrw: true)
                );

                var json = JObject.Parse(body);
                string hexResult = json["result"]?.ToString()?.Replace("0x", "") ?? "0";
                return hexResult;
            }
            catch (Exception ex)
            {
                if (_log) Console.WriteLine($"Request error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Получает Chain ID сети
        /// </summary>
        public async Task<string> ChainId(string rpc, string proxy = "")
        {
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_chainId"", ""params"": [], ""id"": 1 }}";

            try
            {
                string body = await Task.Run(() =>
                    _project.POST(rpc, jsonBody, proxy, useNetHttp: _useNetHttp, log: _log, thrw: true)
                );

                var json = JObject.Parse(body);
                return json["result"]?.ToString() ?? "0x0";
            }
            catch (Exception ex)
            {
                if (_log) Console.WriteLine($"Request error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Получает текущую цену газа
        /// </summary>
        public async Task<string> GasPrice(string rpc, string proxy = "")
        {
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_gasPrice"", ""params"": [], ""id"": 1 }}";

            try
            {
                string body = await Task.Run(() =>
                    _project.POST(rpc, jsonBody, proxy, useNetHttp: _useNetHttp, log: _log, thrw: true)
                );

                var json = JObject.Parse(body);
                return json["result"]?.ToString()?.Replace("0x", "") ?? "0";
            }
            catch (Exception ex)
            {
                if (_log) Console.WriteLine($"Request error: {ex.Message}");
                throw;
            }
        }
    }
}

namespace z3nCore
{
    public static partial class W3bTools
    {
        /// <summary>
        /// Получает баланс нативной монеты
        /// </summary>
        public static decimal EvmNative(
            this IZennoPosterProjectModel project,
            string rpc,
            string address = null,
            string proxy = "",
            bool useNetHttp = false,
            bool log = false)
        {
            if (string.IsNullOrEmpty(address))
                address = project.Var("addressEvm");

            var evmTools = new W3b.EvmTools(project, useNetHttp, log);
            string nativeHex = evmTools.Native(rpc, address, proxy).GetAwaiter().GetResult();
            return nativeHex.ToDecimal();
        }

        /// <summary>
        /// Получает баланс ERC20 токена
        /// </summary>
        public static decimal ERC20(
            this IZennoPosterProjectModel project,
            string tokenContract,
            string rpc,
            string address = null,
            string proxy = "",
            bool useNetHttp = false,
            bool log = false)
        {
            if (string.IsNullOrEmpty(address))
                address = project.Var("addressEvm");

            var evmTools = new W3b.EvmTools(project, useNetHttp, log);
            string balanceHex = evmTools.Erc20(tokenContract, rpc, address, proxy).GetAwaiter().GetResult();
            return balanceHex.ToDecimal();
        }

        /// <summary>
        /// Получает баланс ERC721 NFT
        /// </summary>
        public static decimal ERC721(
            this IZennoPosterProjectModel project,
            string tokenContract,
            string rpc,
            string address = null,
            string proxy = "",
            bool useNetHttp = false,
            bool log = false)
        {
            if (string.IsNullOrEmpty(address))
                address = project.Var("addressEvm");

            var evmTools = new W3b.EvmTools(project, useNetHttp, log);
            string balanceHex = evmTools.Erc721(tokenContract, rpc, address, proxy).GetAwaiter().GetResult();
            return balanceHex.ToDecimal();
        }

        /// <summary>
        /// Получает баланс ERC1155 токена
        /// </summary>
        public static decimal ERC1155(
            this IZennoPosterProjectModel project,
            string tokenContract,
            string tokenId,
            string rpc,
            string address = null,
            string proxy = "",
            bool useNetHttp = false,
            bool log = false)
        {
            if (string.IsNullOrEmpty(address))
                address = project.Var("addressEvm");

            var evmTools = new W3b.EvmTools(project, useNetHttp, log);
            string balanceHex = evmTools.Erc1155(tokenContract, tokenId, rpc, address, proxy).GetAwaiter().GetResult();
            return balanceHex.ToDecimal();
        }

        /// <summary>
        /// Получает текущую цену газа
        /// </summary>
        public static decimal GasPrice(
            this IZennoPosterProjectModel project,
            string rpc,
            string proxy = "",
            bool useNetHttp = false,
            bool log = false)
        {
            var evmTools = new W3b.EvmTools(project, useNetHttp, log);
            string balanceHex = evmTools.GasPrice(rpc, proxy).GetAwaiter().GetResult();
            return balanceHex.ToDecimal(10);
        }

        /// <summary>
        /// Получает nonce (количество транзакций) для адреса
        /// </summary>
        public static int Nonce(
            this IZennoPosterProjectModel project,
            string rpc,
            string address = null,
            string proxy = "",
            bool useNetHttp = false,
            bool log = false)
        {
            if (string.IsNullOrEmpty(address))
                address = project.Var("addressEvm");

            var evmTools = new W3b.EvmTools(project, useNetHttp, log);
            string nonceHex = evmTools.Nonce(rpc, address, proxy).GetAwaiter().GetResult();
            int transactionCount = nonceHex == "0" ? 0 : Convert.ToInt32(nonceHex, 16);
            return transactionCount;
        }

        /// <summary>
        /// Получает Chain ID сети
        /// </summary>
        public static int ChainId(
            this IZennoPosterProjectModel project,
            string rpc,
            string proxy = "",
            bool useNetHttp = false,
            bool log = false)
        {
            var evmTools = new W3b.EvmTools(project, useNetHttp, log);
            string idHex = evmTools.ChainId(rpc, proxy).GetAwaiter().GetResult();
            int id = idHex == "0" ? 0 : Convert.ToInt32(idHex, 16);
            return id;
        }

        /// <summary>
        /// Ожидает выполнения транзакции
        /// </summary>
        public static bool WaitTx(
            this IZennoPosterProjectModel project,
            string rpc,
            string hash,
            int deadline = 60,
            string proxy = "",
            bool useNetHttp = false,
            bool log = false,
            bool extended = false)
        {
            var evmTools = new W3b.EvmTools(project, useNetHttp, log);
            
            if (extended)
                return evmTools.WaitTxExtended(rpc, hash, deadline, proxy).GetAwaiter().GetResult();
            else
                return evmTools.WaitTx(rpc, hash, deadline, proxy).GetAwaiter().GetResult();
        }
    }
}