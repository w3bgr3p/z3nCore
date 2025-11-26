# Документация API z3nCore (Русский)

[![English version](https://img.shields.io/badge/English%20version-available-blue.svg)](../En/)

Полная справка API для библиотеки z3nCore - комплексный набор инструментов для автоматизации ZennoPoster, интеграции с блокчейном и веб-скрапинга.

## Содержание

- [Api - Интеграции с внешними API](#api---интеграции-с-внешними-api)
- [Browser - Автоматизация браузера](#browser---автоматизация-браузера)
- [Core - Инициализация и очистка](#core---инициализация-и-очистка)
- [MethodExtensions - Расширения типов](#methodextensions---расширения-типов)
- [Processess - Управление процессами](#processess---управление-процессами)
- [ProjectExtentions - Утилиты проекта](#projectextentions---утилиты-проекта)
- [Reports - Система отчетов](#reports---система-отчетов)
- [Requests - HTTP клиент](#requests---http-клиент)
- [Security - Криптография и SAFU](#security---криптография-и-safu)
- [Socials - Интеграция соцсетей](#socials---интеграция-соцсетей)
- [Sql - Операции с БД](#sql---операции-с-бд)
- [Utilities - Вспомогательные функции](#utilities---вспомогательные-функции)
- [W3b - Инструменты блокчейна](#w3b---инструменты-блокчейна)
- [Wallets - Интеграция кошельков](#wallets---интеграция-кошельков)
- [Root - Основные утилиты](#root---основные-утилиты)

---

## Api - Интеграции с внешними API

Интеграции с внешними сервисами: AI, криптовалютные биржи, почта и социальные платформы.

- **[AI.md](Api/AI.md)** - Интеграция с AI сервисами (ChatGPT, Claude и др.)
- **[AntiCaptcha.md](Api/AntiCaptcha.md)** - Интеграция сервиса решения CAPTCHA
- **[BinanceApi.md](Api/BinanceApi.md)** - API криптовалютной биржи Binance
- **[Bitget.md](Api/Bitget.md)** - API криптовалютной биржи Bitget
- **[DMail.md](Api/DMail.md)** - Сервис временной почты DMail
- **[DiscordApi.md](Api/DiscordApi.md)** - Интеграция с Discord API
- **[FirstMail.md](Api/FirstMail.md)** - Сервис временной почты FirstMail
- **[Galxe.md](Api/Galxe.md)** - Интеграция с платформой Galxe
- **[GazZip.md](Api/GazZip.md)** - Сервис оценки газа LayerZero
- **[Git.md](Api/Git.md)** - Операции с системой контроля версий Git
- **[GitHub.md](Api/GitHub.md)** - Интеграция с GitHub API
- **[Mexc.md](Api/Mexc.md)** - API криптовалютной биржи MEXC
- **[OKXApi.md](Api/OKXApi.md)** - API криптовалютной биржи OKX
- **[Unlock.md](Api/Unlock.md)** - Интеграция с Unlock Protocol

## Browser - Автоматизация браузера

Автоматизация браузера, управление куками, мониторинг трафика и работа с расширениями.

- **[BrowserScan.md](Browser/BrowserScan.md)** - Снятие отпечатков браузера и утилиты для временных зон
- **[Captcha.md](Browser/Captcha.md)** - Методы решения Cloudflare и CAPTCHA
- **[ChromeExt.md](Browser/ChromeExt.md)** - Управление расширениями Chrome (установка, удаление, включение)
- **[Cookies.md](Browser/Cookies.md)** - Высокопроизводительное управление куками с Base64 хранением
- **[HtmlExtensions.md](Browser/HtmlExtensions.md)** - Декодирование QR-кодов и извлечение хешей транзакций
- **[InstanceExtensions.md](Browser/InstanceExtensions.md)** - Помощники автоматизации браузера (поиск элементов, клики, выполнение JS)
- **[Traffic.md](Browser/Traffic.md)** - Мониторинг и анализ HTTP трафика
- **[ZB.md](Browser/ZB.md)** - Утилиты управления профилями ZennoBrowser

## Core - Инициализация и очистка

Инициализация проекта, управление сессиями и операции очистки.

- **[Disposer.md](Core/Disposer.md)** - Очистка сессии, отчеты, сохранение кук
- **[Init.md](Core/Init.md)** - Инициализация проекта, запуск браузера, загрузка кошельков, выбор аккаунтов

## MethodExtensions - Расширения типов

Методы расширения для встроенных типов C# и объектов ZennoPoster.

- **[ListExtentions.md](MethodExtensions/ListExtentions.md)** - Расширения для List<string> и синхронизация списков проекта
- **[StringExtentions.md](MethodExtensions/StringExtentions.md)** - Крипто-методы, работа с SVG, обработка текста (30+ методов)

## Processess - Управление процессами

Управление процессами и PID для экземпляров браузера.

- **[ProcAcc.md](Processess/ProcAcc.md)** - Менеджер ассоциаций Процесс-Аккаунт (18 методов)
- **[Running.md](Processess/Running.md)** - Межпроцессное хранилище в разделяемой памяти (10 методов)

## ProjectExtentions - Утилиты проекта

Основные утилиты для логирования, управления временем, генерации случайных значений и переменных.

- **[Exceptions.md](ProjectExtentions/Exceptions.md)** - Пользовательские типы исключений и методы выброса
- **[FS.md](ProjectExtentions/FS.md)** - Операции с файловой системой (копирование, удаление, выбор случайного файла)
- **[Logger.md](ProjectExtentions/Logger.md)** - Система логирования с поддержкой эмодзи
- **[Rnd.md](ProjectExtentions/Rnd.md)** - Генерация случайных значений (строки, числа, булевы значения, файлы)
- **[Time.md](ProjectExtentions/Time.md)** - Утилиты времени (прошедшее время, кулдауны, задержки)
- **[Utils.md](ProjectExtentions/Utils.md)** - Общие утилиты (очистка, выполнение ZP, информация о сессии)
- **[Vars.md](ProjectExtentions/Vars.md)** - Управление переменными (получение/установка, счетчики, математические операции)

## Reports - Система отчетов

Отчеты о балансах и уведомления об ошибках/успехе.

- **[Accountant.md](Reports/Accountant.md)** - Таблицы балансов с тепловыми картами
- **[DailyReport.md](Reports/DailyReport.md)** - Ежедневные отчеты фермы с отслеживанием PID
- **[Reporter.md](Reports/Reporter.md)** - Отчеты об ошибках и успехе в лог/Telegram/БД

## Requests - HTTP клиент

Современный HTTP клиент с поддержкой async.

- **[NetHttp.md](Requests/NetHttp.md)** - Асинхронный и синхронный HTTP клиент (GET, POST, PUT, DELETE)

## Security - Криптография и SAFU

Криптографические функции и утилиты безопасности.

- **[Cryptography.md](Security/Cryptography.md)** - AES шифрование, Bech32 кодирование, Blake2b хеширование
- **[ISAFU.md](Security/ISAFU.md)** - Система безопасности SAFU (encode/decode, аппаратные пароли)

## Socials - Интеграция соцсетей

Интеграция с социальными платформами (Discord, GitHub, Google, Twitter, Telegram).

- **[Discord.md](Sql/Db.md)** - Аутентификация Discord и управление серверами
- **[GitHub.md](Sql/PostgreSQL.md)** - Аутентификация GitHub и управление аккаунтом
- **[Google.md](Sql/SQLite.md)** - Аутентификация Google и управление состоянием
- **[Guild.md](Sql/Sql.md)** - Интеграция с Guild.xyz (роли, подключения, SVG)
- **[Telegram.md](Sql/dSql.md)** - Система уведомлений Telegram
- **[X.md](Sql/dSql.md)** - Автоматизация Twitter/X (твиты, лайки, ретвиты, подписки)

## Sql - Операции с БД

Утилиты для работы с базами данных PostgreSQL и SQLite.

- **[Db.md](Sql/Db.md)** - Высокоуровневые операции с БД (35+ методов)
- **[dSql.md](Sql/dSql.md)** - Универсальный асинхронный класс БД с Dapper
- **[PostgreSQL.md](Sql/PostgreSQL.md)** - Прямые операции с PostgreSQL
- **[Sql.md](Sql/Sql.md)** - Унифицированный класс БД на основе экземпляра
- **[SQLite.md](Sql/SQLite.md)** - Операции SQLite через ODBC

## Utilities - Вспомогательные функции

Различные вспомогательные функции для конвертации, отладки, форм, OTP, RSS и скриншотов.

- **[Converter.md](Utilities/Converter.md)** - Конвертация форматов (hex, base64, bech32, bytes)
- **[Debug.md](Utilities/Debug.md)** - Информация о версиях сборок и мониторинг процессов
- **[F0rms.md](Utilities/F0rms.md)** - Интерактивные диалоги Windows Forms (7 методов)
- **[Helper.md](Utilities/Helper.md)** - Поиск в XML документации
- **[OTP.md](Utilities/OTP.md)** - Генерация TOTP кодов (офлайн + FirstMail)
- **[Rss.md](Utilities/Rss.md)** - Парсер RSS новостей для крипто-источников
- **[Sleeper.md](Utilities/Sleeper.md)** - Генератор случайных задержек для имитации человека
- **[Snapper.md](Utilities/Snapper.md)** - Система снимков/резервных копий проекта и DLL

## W3b - Инструменты блокчейна

Комплексная интеграция с блокчейном для множества сетей.

- **[AptosTools.md](W3b/AptosTools.md)** - Инструменты блокчейна Aptos (нативные + токены)
- **[Blockchain.md](W3b/Blockchain.md)** - Основные взаимодействия с EVM блокчейном через Nethereum
- **[CosmosTools.md](W3b/CosmosTools.md)** - Инструменты кошельков Cosmos SDK (деривация адресов)
- **[EvmTools.md](W3b/EvmTools.md)** - Утилиты для EVM-совместимых блокчейнов
- **[Rpc.md](W3b/Rpc.md)** - Справочник RPC эндпоинтов для 40+ сетей
- **[SolTools.md](W3b/SolTools.md)** - Инструменты блокчейна Solana (нативные + SPL токены)
- **[SuiTools.md](W3b/SuiTools.md)** - Инструменты блокчейна Sui и генерация ключей
- **[Tx.md](W3b/Tx.md)** - Высокоуровневое выполнение транзакций с авто-оценкой газа
- **[W3b.md](W3b/W3b.md)** - API цен (CoinGecko, OKX, KuCoin, DexScreener) и утилиты Web3

## Wallets - Интеграция кошельков

Интеграции с браузерными расширениями кошельков.

- **[AuroWallet.md](Wallets/AuroWallet.md)** - Интеграция с кошельком Aurora
- **[BackpackWallet.md](Wallets/BackpackWallet.md)** - Кошелек Backpack (Solana)
- **[Keplr.md](Wallets/Keplr.md)** - Кошелек Keplr (Cosmos)
- **[KeplrWallet.md](Wallets/KeplrWallet.md)** - Расширенный функционал кошелька Keplr
- **[RabbyWallet.md](Wallets/RabbyWallet.md)** - Кошелек Rabby (EVM)
- **[SuietWallet.md](Wallets/SuietWallet.md)** - Кошелек Suiet (Sui)
- **[ZerionWallet.md](Wallets/ZerionWallet.md)** - Кошелек Zerion (EVM)

## Root - Основные утилиты

Утилиты корневого уровня для построения БД, синхронизации с GitHub и тестирования.

- **[DbBuilder.md](DbBuilder.md)** - Создание таблиц БД и формы импорта данных
- **[dSql.md](dSql.md)** - Устаревшая документация dSql (см. Sql/dSql.md для текущей версии)
- **[dsql_alter.md](dsql_alter.md)** - Дополнительная документация по dSql
- **[tGH.md](tGH.md)** - Синхронизация репозиториев GitHub с валидацией размера
- **[Test.md](Test.md)** - Утилиты отчетов о балансах и помощники тестирования

---

## Формат документации

Каждый метод включает:
- **Назначение** - Четкое объяснение того, что делает метод
- **Пример** - Минимальный практический пример кода с контекстом
- **Разбор** - Подробное описание параметров с комментариями внутри кода, объясняющими:
  - Типы и значения параметров
  - Возвращаемые значения
  - Обработка исключений/ошибок
  - Важные замечания и ограничения

---

**Всего:** 147 файлов документации | 36,396+ строк | 300+ публичных методов

[← Назад к главной документации](../README.md) | [English version →](../En/)
