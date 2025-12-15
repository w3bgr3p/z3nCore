
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    

    public static partial class W3bTools 
    {
        
        public static decimal OKXPrice(this IZennoPosterProjectModel project, string tiker)
        {
            tiker = tiker.ToUpper();
            return new OkxApi(project).OKXPrice<decimal>($"{tiker}-USDT");

        }
        public static decimal UsdToToken(this IZennoPosterProjectModel project, decimal usdAmount, string tiker, string apiProvider = "KuCoin")
        {
            decimal price;
            switch (apiProvider)
            {
                case "KuCoin":
                    price = Api.KuCoin.KuPrice(tiker);
                    break;
                case "OKX":
                    price = project.OKXPrice(tiker);
                    break;
                case "CoinGecco":
                    price = Api.CoinGecco.PriceByTiker(tiker);
                    break;
                default:
                    throw new ArgumentException($"unknown method {apiProvider}");
            }
            return usdAmount / price;
        }
        private static decimal ToDecimal(this BigInteger balanceWei, int decimals = 18)
        {
            BigInteger divisor = BigInteger.Pow(10, decimals);
            BigInteger integerPart = balanceWei / divisor;
            BigInteger fractionalPart = balanceWei % divisor;

            decimal result = (decimal)integerPart + ((decimal)fractionalPart / (decimal)divisor);
            return result;
        }
        private static decimal ToDecimal(this string balanceHex, int decimals = 18)
        {
            BigInteger number = BigInteger.Parse("0" + balanceHex, NumberStyles.AllowHexSpecifier);
            return ToDecimal(number, decimals);
        }


    }

    public class W3bLegacy
    {
        protected readonly IZennoPosterProjectModel _project;
        protected readonly Logger _logger;

        public W3bLegacy(IZennoPosterProjectModel project, bool log = false, string key = null)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            _project = project;
            _logger = new Logger(project, log: log, classEmoji: "💠");
        }

        private string ChekAdr(string address)
        {
            if (string.IsNullOrEmpty(address)) address = _project.Var("addressEvm");
            if (string.IsNullOrEmpty(address)) throw new ArgumentException("!W address is nullOrEmpty");
            return address;
        }
        private string CheckRpc(string rpc)
        {
            if (string.IsNullOrEmpty(rpc)) rpc = _project.Var("blockchainRPC");
            if (string.IsNullOrEmpty(rpc)) throw new ArgumentException("!W rpc is nullOrEmpty");
            return rpc;
        }

        private static decimal ToDecimal(BigInteger balanceWei, int decimals = 18)
        {
            BigInteger divisor = BigInteger.Pow(10, decimals);
            BigInteger integerPart = balanceWei / divisor;
            BigInteger fractionalPart = balanceWei % divisor;

            decimal result = (decimal)integerPart + ((decimal)fractionalPart / (decimal)divisor);
            return result;
        }
        private static decimal ToDecimal(string balanceHex)
        {
            BigInteger number = BigInteger.Parse("0" + balanceHex, NumberStyles.AllowHexSpecifier);
            return ToDecimal(number);
        }

        public decimal NativeEvm(string rpc = null, string address = null, string proxy = null, bool log = false)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            address = ChekAdr(address);
            rpc = CheckRpc(rpc);

            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_getBalance"", ""params"": [""{address}"", ""latest""], ""id"": 1 }}";
            string response;
            try
            {
                response = _project.POST(rpc, jsonBody, proxy:proxy, log:log);
            }
            catch (Exception ex)
            {
                _logger.Send(ex.Message, show:true);
                throw ex;
            }

            var json = JObject.Parse(response);
            string hexBalance = json["result"]?.ToString()?.TrimStart('0', 'x') ?? "0";
            BigInteger balanceWei = BigInteger.Parse("0" + hexBalance, NumberStyles.AllowHexSpecifier);
            decimal balance = ToDecimal(hexBalance);
            _logger.Send($"NativeBal: [{balance}] by {rpc} ({address})");
            return balance;

        }

    }

    public static partial class StringExtensions
    {
        public static BigInteger WeiToEth(this string wei, int decimals = 18)
        {
            return BigInteger.Parse(wei, NumberStyles.AllowHexSpecifier);
        }
    }



}
