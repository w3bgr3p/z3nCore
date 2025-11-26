# Документация класса AI

## Обзор
Класс `AI` предоставляет интеграцию с AI-сервисами (Perplexity и AIIO) для генерации текста, оптимизации кода и создания контента.

---

## Конструктор

### `AI(IZennoPosterProjectModel project, string provider, string model = null, bool log = false)`

**Назначение:** Инициализирует AI-клиент с указанным провайдером и настройками модели.

**Пример:**
```csharp
// Инициализация с провайдером Perplexity
var ai = new AI(project, "perplexity", log: true);

// Инициализация с конкретной моделью
var ai = new AI(project, "aiio", "deepseek-ai/DeepSeek-R1-0528", log: true);
```

**Разбор:**
```csharp
var ai = new AI(
    project,      // IZennoPosterProjectModel - экземпляр проекта
    "perplexity", // string - имя провайдера ("perplexity" или "aiio")
    null,         // string - опционально: конкретное имя модели
    true          // bool - включить логирование
);
```

---

## Публичные методы

### `Query(string systemContent, string userContent, string aiModel = "rnd", bool log = false, double temperature_ = 0.8, double top_p_ = 0.9, double top_k_ = 0, int presence_penalty_ = 0, int frequency_penalty_ = 1)`

**Назначение:** Отправляет запрос к AI-сервису и возвращает ответ.

**Пример:**
```csharp
var ai = new AI(project, "perplexity");
string response = ai.Query(
    "Ты полезный помощник",
    "Объясни блокчейн простыми словами"
);
```

**Разбор:**
```csharp
string result = ai.Query(
    "Ты полезный помощник",         // string - системный промпт, определяющий поведение AI
    "Объясни блокчейн",              // string - вопрос/запрос пользователя
    "rnd",                          // string - модель AI ("rnd" для случайного выбора)
    false,                          // bool - включить логирование
    0.8,                            // double - temperature (креативность: 0.0-1.0)
    0.9,                            // double - top_p (nucleus sampling)
    0,                              // double - top_k (разнообразие выбора токенов)
    0,                              // int - presence_penalty (разнообразие тем)
    1                               // int - frequency_penalty (контроль повторений)
);
// Возвращает: string - ответ, сгенерированный AI
// Выбрасывает: Exception - если запрос к API не удался или парсинг ответа не удался
```

---

### `GenerateTweet(string content, string bio = "", bool log = false)`

**Назначение:** Генерирует пост для социальных сетей длиной твита на основе контента и опциональной биографии.

**Пример:**
```csharp
var ai = new AI(project, "aiio");
string tweet = ai.GenerateTweet(
    "Создай пост о Web3 разработке",
    "Блокчейн-разработчик и криптоэнтузиаст"
);
```

**Разбор:**
```csharp
string tweet = ai.GenerateTweet(
    "Создай пост о Web3",       // string - тема контента для твита
    "Блокчейн-разработчик",     // string - опциональная биография для соответствия персоне
    true                         // bool - включить логирование
);
// Возвращает: string - сгенерированный твит (макс. 220 символов)
// Примечание: Автоматически перегенерирует, если твит превышает 220 символов
```

---

### `OptimizeCode(string content, bool log = false)`

**Назначение:** Оптимизирует код с помощью AI, возвращая только оптимизированный код без объяснений.

**Пример:**
```csharp
var ai = new AI(project, "perplexity");
string code = "function test() { var x = 1; var y = 2; return x + y; }";
string optimized = ai.OptimizeCode(code);
```

**Разбор:**
```csharp
string optimizedCode = ai.OptimizeCode(
    "function test() { ... }",  // string - код для оптимизации
    true                         // bool - включить логирование
);
// Возвращает: string - оптимизированный код (чистый текст, без комментариев)
// Примечание: Разработан для оптимизации Web3/блокчейн кода
```

---

### `GoogleAppeal(bool log = false)`

**Назначение:** Генерирует апелляцию для случаев приостановки/блокировки аккаунта Google.

**Пример:**
```csharp
var ai = new AI(project, "aiio");
string appeal = ai.GoogleAppeal();
// Используйте сгенерированную апелляцию для отправки в поддержку Google
```

**Разбор:**
```csharp
string appealMessage = ai.GoogleAppeal(
    false  // bool - включить логирование
);
// Возвращает: string - сгенерированное сообщение апелляции (~200 символов)
// Примечание: Создает скромную, нетехническую апелляцию с объяснением ситуации
```

---

## Примечания

- API ключ извлекается из базы данных с помощью `project.SqlGet()` на основе провайдера
- Для выбора модели "rnd" выбирается случайная модель из предопределенного списка
- Все методы используют JSON для обработки запросов/ответов
- Ответы логируются, когда параметр log включен
