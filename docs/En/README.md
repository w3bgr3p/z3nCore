# z3nCore API Documentation (English)

[![Русская версия](https://img.shields.io/badge/Русская%20версия-доступна-red.svg)](../Ru/)

Complete API reference for the z3nCore library - a comprehensive toolkit for ZennoPoster automation, blockchain integration, and web scraping.

## Table of Contents

- [Api - External API Integrations](#api---external-api-integrations)
- [Browser - Browser Automation](#browser---browser-automation)
- [Core - Initialization & Cleanup](#core---initialization--cleanup)
- [MethodExtensions - Type Extensions](#methodextensions---type-extensions)
- [Processess - Process Management](#processess---process-management)
- [ProjectExtentions - Project Utilities](#projectextentions---project-utilities)
- [Reports - Reporting System](#reports---reporting-system)
- [Requests - HTTP Client](#requests---http-client)
- [Security - Cryptography & SAFU](#security---cryptography--safu)
- [Socials - Social Platform Integration](#socials---social-platform-integration)
- [Sql - Database Operations](#sql---database-operations)
- [Utilities - Helper Functions](#utilities---helper-functions)
- [W3b - Blockchain Tools](#w3b---blockchain-tools)
- [Wallets - Browser Wallet Integration](#wallets---browser-wallet-integration)
- [Root - Core Utilities](#root---core-utilities)

---

## Api - External API Integrations

External service integrations for AI, cryptocurrency exchanges, email, and social platforms.

- **[AI.md](Api/AI.md)** - AI service integration (ChatGPT, Claude, etc.)
- **[AntiCaptcha.md](Api/AntiCaptcha.md)** - CAPTCHA solving service integration
- **[BinanceApi.md](Api/BinanceApi.md)** - Binance cryptocurrency exchange API
- **[Bitget.md](Api/Bitget.md)** - Bitget cryptocurrency exchange API
- **[DMail.md](Api/DMail.md)** - DMail temporary email service
- **[DiscordApi.md](Api/DiscordApi.md)** - Discord API integration
- **[FirstMail.md](Api/FirstMail.md)** - FirstMail temporary email service
- **[Galxe.md](Api/Galxe.md)** - Galxe platform integration
- **[GazZip.md](Api/GazZip.md)** - LayerZero gas estimation service
- **[Git.md](Api/Git.md)** - Git version control operations
- **[GitHub.md](Api/GitHub.md)** - GitHub API integration
- **[Mexc.md](Api/Mexc.md)** - MEXC cryptocurrency exchange API
- **[OKXApi.md](Api/OKXApi.md)** - OKX cryptocurrency exchange API
- **[Unlock.md](Api/Unlock.md)** - Unlock Protocol integration

## Browser - Browser Automation

Browser automation, cookie management, traffic monitoring, and extension handling.

- **[BrowserScan.md](Browser/BrowserScan.md)** - Browser fingerprinting and timezone utilities
- **[Captcha.md](Browser/Captcha.md)** - Cloudflare and CAPTCHA solving methods
- **[ChromeExt.md](Browser/ChromeExt.md)** - Chrome extension management (install, remove, enable)
- **[Cookies.md](Browser/Cookies.md)** - High-performance cookie management with Base64 storage
- **[HtmlExtensions.md](Browser/HtmlExtensions.md)** - QR code decoding and transaction hash extraction
- **[InstanceExtensions.md](Browser/InstanceExtensions.md)** - Browser automation helpers (element finding, clicking, JS execution)
- **[Traffic.md](Browser/Traffic.md)** - HTTP traffic monitoring and analysis
- **[ZB.md](Browser/ZB.md)** - ZennoBrowser profile management utilities

## Core - Initialization & Cleanup

Project initialization, session management, and cleanup operations.

- **[Disposer.md](Core/Disposer.md)** - Session cleanup, reports, cookie saving
- **[Init.md](Core/Init.md)** - Project initialization, browser launch, wallet loading, account selection

## MethodExtensions - Type Extensions

Extension methods for built-in C# types and ZennoPoster objects.

- **[ListExtentions.md](MethodExtensions/ListExtentions.md)** - List<string> extensions and project list synchronization
- **[StringExtentions.md](MethodExtensions/StringExtentions.md)** - Crypto methods, SVG handling, text processing (30+ methods)

## Processess - Process Management

Process and PID management for browser instances.

- **[ProcAcc.md](Processess/ProcAcc.md)** - Process-Account association manager (18 methods)
- **[Running.md](Processess/Running.md)** - Inter-process shared memory storage (10 methods)

## ProjectExtentions - Project Utilities

Core utilities for logging, time management, random generation, and variables.

- **[Exceptions.md](ProjectExtentions/Exceptions.md)** - Custom exception types and throwing methods
- **[FS.md](ProjectExtentions/FS.md)** - File system operations (copy, delete, random file selection)
- **[Logger.md](ProjectExtentions/Logger.md)** - Logging system with emoji support
- **[Rnd.md](ProjectExtentions/Rnd.md)** - Random generation (strings, numbers, booleans, files)
- **[Time.md](ProjectExtentions/Time.md)** - Time utilities (elapsed time, cooldowns, delays)
- **[Utils.md](ProjectExtentions/Utils.md)** - General utilities (cleanup, ZP execution, session info)
- **[Vars.md](ProjectExtentions/Vars.md)** - Variable management (get/set, counters, math operations)

## Reports - Reporting System

Balance reporting and error/success notifications.

- **[Accountant.md](Reports/Accountant.md)** - Balance tables with heatmaps
- **[DailyReport.md](Reports/DailyReport.md)** - Daily farm reports with PID tracking
- **[Reporter.md](Reports/Reporter.md)** - Error and success reporting to log/Telegram/DB

## Requests - HTTP Client

Modern HTTP client with async support.

- **[NetHttp.md](Requests/NetHttp.md)** - Async and sync HTTP client (GET, POST, PUT, DELETE)

## Security - Cryptography & SAFU

Cryptographic functions and security utilities.

- **[Cryptography.md](Security/Cryptography.md)** - AES encryption, Bech32 encoding, Blake2b hashing
- **[ISAFU.md](Security/ISAFU.md)** - SAFU security system (encode/decode, hardware passwords)

## Socials - Social Platform Integration

Integration with social platforms (Discord, GitHub, Google, Twitter, Telegram).

- **[Discord.en.md](Socials/Discord.en.md)** - Discord authentication and server management
- **[GitHub.en.md](Socials/GitHub.en.md)** - GitHub authentication and account management
- **[Google.en.md](Socials/Google.en.md)** - Google authentication and state management
- **[Guild.en.md](Socials/Guild.en.md)** - Guild.xyz integration (roles, connections, SVG)
- **[Telegram.en.md](Socials/Telegram.en.md)** - Telegram notification system
- **[X.en.md](Socials/X.en.md)** - Twitter/X automation (tweets, likes, retweets, follows)

## Sql - Database Operations

Database utilities for PostgreSQL and SQLite.

- **[Db.md](Sql/Db.md)** - High-level database operations (35+ methods)
- **[dSql.md](Sql/dSql.md)** - Universal async database class with Dapper
- **[PostgreSQL.md](Sql/PostgreSQL.md)** - PostgreSQL direct operations
- **[Sql.md](Sql/Sql.md)** - Instance-based unified database class
- **[SQLite.md](Sql/SQLite.md)** - SQLite operations via ODBC

## Utilities - Helper Functions

Various utility functions for conversion, debugging, forms, OTP, RSS, and screenshots.

- **[Converter.md](Utilities/Converter.md)** - Format conversion (hex, base64, bech32, bytes)
- **[Debug.md](Utilities/Debug.md)** - Assembly version info and process monitoring
- **[F0rms.md](Utilities/F0rms.md)** - Interactive Windows Forms dialogs (7 methods)
- **[Helper.md](Utilities/Helper.md)** - XML documentation search
- **[OTP.md](Utilities/OTP.md)** - TOTP code generation (offline + FirstMail)
- **[Rss.md](Utilities/Rss.md)** - RSS news parser for crypto sources
- **[Sleeper.md](Utilities/Sleeper.md)** - Random delay generator for human-like behavior
- **[Snapper.md](Utilities/Snapper.md)** - Project and DLL snapshot/backup system

## W3b - Blockchain Tools

Comprehensive blockchain integration for multiple networks.

- **[AptosTools.md](W3b/AptosTools.md)** - Aptos blockchain tools (native + token balances)
- **[Blockchain.md](W3b/Blockchain.md)** - Core EVM blockchain interactions with Nethereum
- **[CosmosTools.md](W3b/CosmosTools.md)** - Cosmos SDK wallet tools (address derivation)
- **[EvmTools.md](W3b/EvmTools.md)** - EVM-compatible blockchain utilities
- **[Rpc.md](W3b/Rpc.md)** - RPC endpoint directory for 40+ networks
- **[SolTools.md](W3b/SolTools.md)** - Solana blockchain tools (native + SPL tokens)
- **[SuiTools.md](W3b/SuiTools.md)** - Sui blockchain tools and key generation
- **[Tx.md](W3b/Tx.md)** - High-level transaction execution with auto gas estimation
- **[W3b.md](W3b/W3b.md)** - Price APIs (CoinGecko, OKX, KuCoin, DexScreener) and Web3 utilities

## Wallets - Browser Wallet Integration

Browser extension wallet integrations.

- **[AuroWallet.md](Wallets/AuroWallet.md)** - Aurora wallet integration
- **[BackpackWallet.md](Wallets/BackpackWallet.md)** - Backpack wallet (Solana)
- **[Keplr.md](Wallets/Keplr.md)** - Keplr wallet (Cosmos)
- **[KeplrWallet.md](Wallets/KeplrWallet.md)** - Keplr wallet extended functionality
- **[RabbyWallet.md](Wallets/RabbyWallet.md)** - Rabby wallet (EVM)
- **[SuietWallet.md](Wallets/SuietWallet.md)** - Suiet wallet (Sui)
- **[ZerionWallet.md](Wallets/ZerionWallet.md)** - Zerion wallet (EVM)

## Root - Core Utilities

Root-level utilities for database building, GitHub sync, and testing.

- **[DbBuilder.md](DbBuilder.md)** - Database table creation and data import forms
- **[dSql.md](dSql.md)** - Legacy dSql documentation (see Sql/dSql.md for current version)
- **[tGH.md](tGH.md)** - GitHub repository synchronization with size validation
- **[Test.md](Test.md)** - Balance reporting utilities and testing helpers

---

## Documentation Format

Each method includes:
- **Purpose** - Clear explanation of what the method does
- **Example** - Minimal, practical code example with context
- **Breakdown** - Detailed parameter descriptions with inline comments explaining:
  - Parameter types and meanings
  - Return values
  - Exceptions/error handling
  - Important notes and limitations

---

**Total:** 147 documentation files | 36,396+ lines | 300+ public methods

[← Back to main documentation](../README.md) | [Русская версия →](../Ru/)
