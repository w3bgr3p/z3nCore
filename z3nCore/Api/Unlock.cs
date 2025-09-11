using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZennoLab.InterfacesLibrary.Enums.Log;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public class Unlock
    {

        protected readonly IZennoPosterProjectModel _project;
        protected readonly bool _logShow;
        protected readonly Sql _sql;
        protected readonly string _jsonRpc;
        protected readonly Blockchain _blockchain;
        protected readonly string _abi = @"[
                        {
                            ""inputs"": [
                            {
                                ""internalType"": ""uint256"",
                                ""name"": ""_tokenId"",
                                ""type"": ""uint256""
                            }
                            ],
                            ""name"": ""keyExpirationTimestampFor"",
                            ""outputs"": [
                            {
                                ""internalType"": ""uint256"",
                                ""name"": """",
                                ""type"": ""uint256""
                            }
                            ],
                            ""stateMutability"": ""view"",
                            ""type"": ""function""
                        },
                        {
                            ""inputs"": [
                            {
                                ""internalType"": ""uint256"",
                                ""name"": ""_tokenId"",
                                ""type"": ""uint256""
                            }
                            ],
                            ""name"": ""ownerOf"",
                            ""outputs"": [
                            {
                                ""internalType"": ""address"",
                                ""name"": """",
                                ""type"": ""address""
                            }
                            ],
                            ""stateMutability"": ""view"",
                            ""type"": ""function""
                        }
                    ]";


        public Unlock(IZennoPosterProjectModel project, bool log = false)
        {
            _project = project;
            _sql = new Sql(_project);
            _logShow = log;
            _jsonRpc = Rpc.Get("optimism");
            _blockchain = new Blockchain(_jsonRpc);
        }

        public string keyExpirationTimestampFor(string addressTo, int tokenId, bool decode = true)
        {
            try
            {
                string[] types = { "uint256" };
                object[] values = { tokenId };

                string result = _blockchain.ReadContract(addressTo, "keyExpirationTimestampFor", _abi, values).Result;
                if (decode) result = ProcessExpirationResult(result);
                return result;
            }
            catch (Exception ex)
            {
                _project.L0g(ex.InnerException?.Message ?? ex.Message);
                throw;
            }
        }

        public string ownerOf(string addressTo, int tokenId, bool decode = true)
        {
            try
            {
                string[] types = { "uint256" };
                object[] values = { tokenId };
                string result = _blockchain.ReadContract(addressTo, "ownerOf", _abi, values).Result;
                if (decode) result = Decode(result, "ownerOf");
                return result;
            }
            catch (Exception ex)
            {
                _project.L0g(ex.InnerException?.Message ?? ex.Message);
                throw;
            }
        }

        public string Decode(string toDecode, string function)
        {
            if (string.IsNullOrEmpty(toDecode))
            {
                _project.L0g("Result is empty, nothing to decode");
                return string.Empty;
            }

            if (toDecode.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) toDecode = toDecode.Substring(2);
            if (toDecode.Length < 64) toDecode = toDecode.PadLeft(64, '0');


            var decodedDataExpire = z3nCore.Decoder.AbiDataDecode(_abi, function, "0x" + toDecode);
            string decodedResultExpire = decodedDataExpire.Count == 1
                ? decodedDataExpire.First().Value
                : string.Join("\n", decodedDataExpire.Select(item => $"{item.Key};{item.Value}"));

            return decodedResultExpire;
        }

        string ProcessExpirationResult(string resultExpire)
        {
            if (string.IsNullOrEmpty(resultExpire))
            {
                _project.SendToLog("Result is empty, nothing to decode", LogType.Warning, true, LogColor.Yellow);
                return string.Empty;
            }

            if (resultExpire.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                resultExpire = resultExpire.Substring(2);
            }

            if (resultExpire.Length < 64)
            {
                resultExpire = resultExpire.PadLeft(64, '0');
            }

            var decodedDataExpire = z3nCore.Decoder.AbiDataDecode(_abi, "keyExpirationTimestampFor", "0x" + resultExpire);
            string decodedResultExpire = decodedDataExpire.Count == 1
                ? decodedDataExpire.First().Value
                : string.Join("\n", decodedDataExpire.Select(item => $"{item.Key};{item.Value}"));

            return decodedResultExpire;
        }

        public Dictionary<string, string> Holders(string contract)
        {
            var result = new Dictionary<string, string>();
            int i = 0;
            while (true)
            {
                i++;
                var owner = ownerOf(contract, i);
                if (owner == "0x0000000000000000000000000000000000000000") break;
                var exp = keyExpirationTimestampFor(contract, i);
                result.Add(owner.ToLower(), exp.ToLower());
            }
            return result;


        }

    }
}
