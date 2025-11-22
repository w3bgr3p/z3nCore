# Документация класса GitHub

## Обзор
Класс `GitHub` предоставляет полную интеграцию с GitHub API для управления репозиториями, соавторами и настройками репозитория как для личных аккаунтов, так и для организаций.

---

## Конструкторы

### `GitHub(string token, string username, string organization = null)`

**Назначение:** Инициализирует клиент GitHub API с аутентификацией.

**Пример:**
```csharp
// Личный аккаунт
var github = new GitHub("ghp_token...", "username");

// Организация
var github = new GitHub("ghp_token...", "username", "my-org");
```

**Разбор:**
```csharp
var github = new GitHub(
    "ghp_token...",    // string - персональный токен доступа GitHub
    "username",        // string - имя пользователя GitHub
    "organization"     // string - опциональное название организации
);
// Примечание: HttpClient автоматически настраивается с базовым URL GitHub API
```

---

## Публичные методы

### `GetRepositoryInfo(string repoName)`

**Назначение:** Получает детальную информацию о репозитории.

**Пример:**
```csharp
var github = new GitHub(token, username);
string repoInfo = github.GetRepositoryInfo("my-repo");
// Возвращает JSON с деталями репо: звезды, форки, язык и т.д.
```

**Разбор:**
```csharp
string repositoryInfo = github.GetRepositoryInfo(
    "my-repo"  // string - название репозитория
);
// Возвращает: string - JSON ответ с метаданными репозитория
// Формат ошибки: "Error: {message}"
```

---

### `GetCollaborators(string repoName)`

**Назначение:** Перечисляет всех соавторов репозитория.

**Пример:**
```csharp
var github = new GitHub(token, username);
string collaborators = github.GetCollaborators("my-repo");
// Возвращает JSON массив соавторов с правами
```

**Разбор:**
```csharp
string collaboratorsList = github.GetCollaborators(
    "my-repo"  // string - название репозитория
);
// Возвращает: string - JSON массив объектов соавторов
// Каждый объект включает: login, permissions, role_name
```

---

### `CreateRepository(string repoName)`

**Назначение:** Создает новый приватный репозиторий.

**Пример:**
```csharp
var github = new GitHub(token, username);
string result = github.CreateRepository("new-project");

// Для организации
var github = new GitHub(token, username, "my-org");
string result = github.CreateRepository("org-project");
```

**Разбор:**
```csharp
string createResult = github.CreateRepository(
    "new-repo"  // string - название для нового репозитория
);
// Возвращает: string - JSON ответ с деталями созданного репо
// Создается как: Приватный репозиторий по умолчанию
// Местоположение: Личный аккаунт или организация (зависит от конструктора)
```

---

### `ChangeVisibility(string repoName, bool makePrivate)`

**Назначение:** Изменяет видимость репозитория между публичным и приватным.

**Пример:**
```csharp
var github = new GitHub(token, username);

// Сделать приватным
string result = github.ChangeVisibility("my-repo", true);

// Сделать публичным
string result = github.ChangeVisibility("my-repo", false);
```

**Разбор:**
```csharp
string changeResult = github.ChangeVisibility(
    "my-repo",  // string - название репозитория
    true        // bool - true=приватный, false=публичный
);
// Возвращает: string - JSON ответ с обновленными настройками репо
// Примечание: Требуются соответствующие права
```

---

### `AddCollaborator(string repoName, string collaboratorUsername, string permission = "pull")`

**Назначение:** Добавляет соавтора в репозиторий с конкретными правами.

**Пример:**
```csharp
var github = new GitHub(token, username);

// Добавить с доступом только для чтения
github.AddCollaborator("my-repo", "johndoe", "pull");

// Добавить с доступом на запись
github.AddCollaborator("my-repo", "janedoe", "push");

// Добавить с правами администратора
github.AddCollaborator("my-repo", "admin-user", "admin");
```

**Разбор:**
```csharp
string addResult = github.AddCollaborator(
    "my-repo",     // string - название репозитория
    "username",    // string - имя пользователя GitHub соавтора
    "pull"         // string - уровень прав
);
// Возвращает: string - JSON ответ
// Права: "pull" (чтение), "push" (запись), "admin", "maintain", "triage"
```

---

### `RemoveCollaborator(string repoName, string collaboratorUsername)`

**Назначение:** Удаляет соавтора из репозитория.

**Пример:**
```csharp
var github = new GitHub(token, username);
string result = github.RemoveCollaborator("my-repo", "johndoe");
```

**Разбор:**
```csharp
string removeResult = github.RemoveCollaborator(
    "my-repo",     // string - название репозитория
    "username"     // string - соавтор для удаления
);
// Возвращает: string - JSON ответ подтверждающий удаление
```

---

### `ChangeCollaboratorPermission(string repoName, string collaboratorUsername, string permission = "pull")`

**Назначение:** Обновляет уровень прав существующего соавтора.

**Пример:**
```csharp
var github = new GitHub(token, username);

// Повысить до админа
string result = github.ChangeCollaboratorPermission(
    "my-repo",
    "johndoe",
    "admin"
);
```

**Разбор:**
```csharp
string changeResult = github.ChangeCollaboratorPermission(
    "my-repo",     // string - название репозитория
    "username",    // string - имя пользователя соавтора
    "push"         // string - новый уровень прав
);
// Возвращает: "Success: Permission updated" или JSON ответ
// Валидные права: "pull", "push", "admin", "maintain", "triage"
// Возвращает ошибку если предоставлены невалидные права
```

---

## Уровни прав

| Право | Уровень доступа | Описание |
|-------|----------------|----------|
| pull | Чтение | Может читать и клонировать |
| triage | Сортировка | Может управлять issues/PRs |
| push | Запись | Может пушить изменения |
| maintain | Поддержка | Может управлять репо (не админ) |
| admin | Админ | Полный доступ к репозиторию |

---

## Используемые эндпоинты API

Все эндпоинты относительно `https://api.github.com/`:

- `GET repos/{owner}/{repo}` - Информация о репозитории
- `GET repos/{owner}/{repo}/collaborators` - Список соавторов
- `POST user/repos` - Создать репо (личный)
- `POST orgs/{org}/repos` - Создать репо (организация)
- `PATCH repos/{owner}/{repo}` - Обновить настройки репо
- `PUT repos/{owner}/{repo}/collaborators/{username}` - Добавить/обновить соавтора
- `DELETE repos/{owner}/{repo}/collaborators/{username}` - Удалить соавтора

---

## Аутентификация

Все запросы включают:
```
Authorization: token {your_github_token}
User-Agent: GitHubManagerApp
```

---

## Обработка ошибок

Все методы возвращают сообщения об ошибках в формате:
```
"Error: {error_message}"
```

Проверяйте ответ на префикс "Error:" для обнаружения сбоев.

---

## Примечания

- Токен должен иметь соответствующие области (`repo`, `admin:org` для организаций)
- HttpClient автоматически обрабатывает базовый URL и заголовки
- Все операции используют синхронные вызовы `.Result`
- Заголовок User-Agent требуется GitHub API
- Название организации в конструкторе направляет операции создания в организацию
- Видимость репозитория по умолчанию: Приватный
- Все HTTP методы используют JSON тип контента
