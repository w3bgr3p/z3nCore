using System.Numerics;
using System;
using System.Text;
using ZennoLab.InterfacesLibrary.ProjectModel;


namespace z3nCore.W3b
{
    public class Faucet
    {
        private readonly string _rpc = Rpc.Sepolia;
        private readonly IZennoPosterProjectModel _project;
        private readonly string _key;
        private readonly Logger _log;
        private readonly string _address;
        private string _faucetContract;

        

        public Faucet(IZennoPosterProjectModel project, string faucetContract, bool _showLog = false)
        {
            _project = project;
            _log = new Logger(project,_showLog,"Zaffier", persistent:true);
            _key = project.DbKey("evm");
            _address = _key.ToEvmAddress();
            _faucetContract = faucetContract;
        }

        
        public void Request()
        {
    
            // Формируем data для requestTokens()
            var data = "0x359cf2b7"; // function selector для requestTokens()
    
            // Отправляем транзакцию
            var hash = new Tx(_project).SendTx(
                _rpc, 
                _faucetContract,  // адрес контракта
                data,            // данные функции
                0,               // value = 0 (не отправляем ETH)
                _key,            // приватный ключ
                0,               // gas multiplier
                1,              // max retries
                true             // wait for confirmation
            );
    
            bool result = _project.WaitTx(_rpc, hash);
            _log.Send($"Request tokens from faucet: {result}: {hash}");
        }
        public void Deposit(decimal ethAmount)
        {
    
            var data = "0xd0e30db0"; // deposit()
    
            var hash = new Tx(_project).SendTx(
                _rpc,
                _faucetContract,
                data,
                ethAmount,  // отправляем ETH
                _key,
                0,               // gas multiplier
                1,   
                true
            );
    
            bool result = _project.WaitTx(_rpc, hash);
            _log.Send($"Deposit {ethAmount} ETH to faucet: {result}: {hash}");
        }
        public void SetWithdrawAmount(decimal ethAmount)
        {
            
            // setWithdrawAmount(uint256)
            var dataBuilder = new StringBuilder();
            dataBuilder.Append("0xceb04e29"); // function selector
            
            // Конвертируем ETH в wei
            BigInteger amountWei = DecimalToWei(ethAmount, 18);
            byte[] bytes = amountWei.ToByteArray();
            Array.Reverse(bytes);
            string amountInHex = BitConverter.ToString(bytes).Replace("-", "").ToLower().PadLeft(64, '0');
            
            dataBuilder.Append(amountInHex);
            
            var data = dataBuilder.ToString();
            
            var hash = new Tx(_project).SendTx(
                _rpc,
                _faucetContract,
                data,
                0,
                _key,
                0,               // gas multiplier
                1,   
                true
            );
            
            bool result = _project.WaitTx(_rpc, hash);
            _log.Send($"Set withdraw amount to {ethAmount} ETH: {result}: {hash}");
        }
        public void SetCooldownTime(uint timeInSeconds)
        {
            
            // setCooldownTime(uint256)
            var dataBuilder = new StringBuilder();
            dataBuilder.Append("0x6ff73201"); // function selector
            
            // Конвертируем время в hex
            string timeHex = timeInSeconds.ToString("X").PadLeft(64, '0');
            dataBuilder.Append(timeHex);
            
            var data = dataBuilder.ToString();
            
            var hash = new Tx(_project).SendTx(
                _rpc,
                _faucetContract,
                data,
                0,
                _key,
                0,               // gas multiplier
                1,   
                true
            );
            
            bool result = _project.WaitTx(_rpc, hash);
            _log.Send($"Set cooldown time to {timeInSeconds} seconds: {result}: {hash}");
        }
        public void WithdrawAllFromFaucet()
        {
            
            // withdrawAll()
            var data = "0x853828b6"; // function selector
            
            var hash = new Tx(_project).SendTx(
                _rpc,
                _faucetContract,
                data,
                0,
                _key,
                0,               // gas multiplier
                1,   
                true
            );
            
            bool result = _project.WaitTx(_rpc, hash);
            _log.Send($"Withdraw all from faucet: {result}: {hash}");
        }
        BigInteger DecimalToWei(decimal amount, int decimals) {
            return new BigInteger(amount * (decimal)Math.Pow(10, decimals));
        }

        
    }
}