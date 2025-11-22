# Captcha

Методы расширения для работы с защитой Cloudflare и решения CAPTCHA.

## Методы расширения

### CFSolve()

```csharp
public static void CFSolve(this Instance instance)
```

**Назначение**: Решает задачи Cloudflare на текущей странице.

**Пример**:
```csharp
instance.Go("https://example.com");
instance.CFSolve(); // Решает задачу Cloudflare, если она присутствует
```

**Детали**:
- `instance` - Экземпляр браузера с задачей Cloudflare
- Автоматически обнаруживает и решает защиту Cloudflare
- Без возвращаемого значения
- Ожидает, пока задача не будет решена

---

### CFToken()

```csharp
public static string CFToken(this Instance instance, int deadline = 60, bool strict = false)
```

**Назначение**: Извлекает токен clearance Cloudflare из cookies после решения задачи.

**Пример**:
```csharp
instance.Go("https://protected-site.com");
string token = instance.CFToken(deadline: 120);
// Возвращает: "cf_clearance=abc123..."
```

**Детали**:
- `instance` - Экземпляр браузера
- `deadline` - Максимальное время ожидания в секундах (по умолчанию: 60)
- `strict` - Использовать строгую валидацию (по умолчанию: false)
- Возвращает токен clearance Cloudflare в виде строки
- Выбрасывает исключение таймаута, если токен не получен в течение deadline

---

### CapGuru()

```csharp
public static bool CapGuru(this IZennoPosterProjectModel project)
```

**Назначение**: Интеграция с CapMonster/CapGuru для автоматического решения CAPTCHA.

**Пример**:
```csharp
if (project.CapGuru())
{
    project.log("CAPTCHA успешно решена");
}
else
{
    project.log("Не удалось решить CAPTCHA");
}
```

**Детали**:
- `project` - Модель проекта ZennoPoster
- Возвращает true, если CAPTCHA успешно решена
- Возвращает false, если решение CAPTCHA не удалось
- Требует настройки сервиса CapMonster/CapGuru
