# Rpc

Статический класс, предоставляющий URL RPC эндпоинтов для различных блокчейн сетей.

---

## Get

### Назначение
Получает RPC URL для блокчейн сети по имени (нечувствительно к регистру).

### Пример
```csharp
string ethRpc = Rpc.Get("Ethereum");
string baseRpc = Rpc.Get("base"); // Нечувствительно к регистру
string zkRpc = Rpc.Get("zksync");
```

### Разбор
```csharp
public static string Get(
    string name    // Имя сети (нечувствительно к регистру)
)
// Возвращает: RPC URL как строку
// Выбрасывает: ArgumentException если имя сети не найдено
// Поддерживает: вариации с подчеркиванием/пробелом (например, "Solana_Devnet" или "SolanaDevnet")
```

---

## Статические свойства

### Назначение
Прямой доступ к RPC URL через статические свойства.

### Пример
```csharp
// Ethereum & L2
string eth = Rpc.Ethereum;           // https://ethereum-rpc.publicnode.com
string arbitrum = Rpc.Arbitrum;      // https://arbitrum-one.publicnode.com
string base = Rpc.Base;              // https://base-rpc.publicnode.com
string optimism = Rpc.Optimism;      // https://optimism-rpc.publicnode.com
string zksync = Rpc.Zksync;          // https://mainnet.era.zksync.io

// Другие EVM сети
string bsc = Rpc.Bsc;                // https://bsc-rpc.publicnode.com
string polygon = Rpc.Polygon;        // https://polygon-rpc.com
string avalanche = Rpc.Avalanche;    // https://avalanche-c-chain.publicnode.com

// Не-EVM сети
string aptos = Rpc.Aptos;            // https://fullnode.mainnet.aptoslabs.com/v1
string solana = Rpc.Solana;          // https://api.mainnet-beta.solana.com
```

### Разбор
```csharp
// Mainnet RPC (EVM)
public static string Ethereum      // Ethereum mainnet
public static string Arbitrum      // Arbitrum One
public static string Base          // Base
public static string Blast         // Blast
public static string Fantom        // Fantom Opera
public static string Linea         // Linea
public static string Manta         // Manta Pacific
public static string Optimism      // Optimism
public static string Scroll        // Scroll
public static string Soneium       // Soneium
public static string Taiko         // Taiko
public static string Unichain      // Unichain
public static string Zero          // Zero Network
public static string Zksync        // zkSync Era
public static string Zora          // Zora

// Другие mainnet (EVM)
public static string Avalanche     // Avalanche C-Chain
public static string Bsc           // Binance Smart Chain
public static string Gravity       // Gravity
public static string Opbnb         // opBNB
public static string Polygon       // Polygon

// Не-EVM сети
public static string Aptos         // Aptos mainnet
public static string Movement      // Movement mainnet
public static string Solana        // Solana mainnet
public static string Solana_Devnet // Solana devnet
public static string Solana_Testnet // Solana testnet

// Testnets
public static string Sepolia       // Ethereum Sepolia testnet
public static string NeuraTestnet  // Neura testnet
public static string MonadTestnet  // Monad testnet
```

---

## Доступные сети

### Mainnet (EVM)
- **Ethereum**: Ethereum mainnet
- **Arbitrum**: Arbitrum One L2
- **Base**: L2 от Coinbase
- **Blast**: Blast L2
- **Fantom**: Fantom Opera
- **Linea**: Linea zkEVM
- **Manta**: Manta Pacific
- **Optimism**: Optimism L2
- **Scroll**: Scroll zkEVM
- **Soneium**: Soneium
- **Taiko**: Taiko
- **Unichain**: L2 от Uniswap
- **Zero**: Zero Network
- **Zksync**: zkSync Era
- **Zora**: Zora Network
- **Avalanche**: Avalanche C-Chain
- **Bsc**: Binance Smart Chain
- **Gravity**: Gravity
- **Opbnb**: opBNB
- **Polygon**: Polygon PoS

### Не-EVM сети
- **Aptos**: Aptos mainnet
- **Movement**: Movement mainnet
- **Solana**: Solana mainnet
- **Solana_Devnet**: Solana devnet
- **Solana_Testnet**: Solana testnet

### Testnets
- **Sepolia**: Ethereum Sepolia
- **NeuraTestnet**: Neura testnet
- **MonadTestnet**: Monad testnet
