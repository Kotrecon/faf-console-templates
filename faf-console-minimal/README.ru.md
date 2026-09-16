# Faf.Console.Minimal — Minimal Console Boilerplate

Шаблон консольного приложения для проверки гипотез на 2–10 классах. Один сценарий — одна папка.

---

## Назначение

Шаблон предназначен для проверки технических гипотез в консольном приложении. Целевой объём — от двух до десяти классов. Один сценарий размещается в одной папке.

---

## Граница применимости

Шаблон завершается там, где начинается `Result<T>`, `CancellationToken` из хоста, `appsettings`, кастомные форматтеры логирования, HTTP и прочая инфраструктура.

---

## Структура файлов

```bash
/
├── README.md                      # английская версия
├── README.ru.md                   # русская версия
├── Program.cs                     # оркестрация: host, try/catch/finally, exit code
├── AppRunner.cs                   # точка входа в сценарий, резолвится из DI
├── Features/
│   └── <Hypothesis>/              # один сценарий — одна папка
│       ├── <классы сценария>
│       └── ...
├── Properties/
│   └── launchSettings.json        # DOTNET_ENVIRONMENT=Development
├── Directory.Build.props          # nullable, warnings-as-errors, LangVersion
└── <project>.csproj
```

**Правило размещения.** Все классы, относящиеся к сценарию, размещаются в `Features/<Hypothesis>/`. В корне остаются только `Program.cs`, `AppRunner.cs` и файлы сборки. Удаление гипотезы сводится к удалению одной папки.

Внутри фичи классы размещаются плоско, пока их количество не превышает 7–8. При превышении — группировка по назначению внутри той же папки.

---

## Схема взаимосвязей

```bash
Program.cs (top-level)
│
├── создаёт ────► LoggerFactory (ранний логгер)
│                 │
│                 └── используется Program.cs для логирования в catch
│
├── собирает ───► Host.CreateApplicationBuilder
│                 │
│                 ├── даёт ──► ILogger (AddSimpleConsole)
│                 │
│                 └── даёт ──► IServiceProvider
│                                │
│                                └── инжектит ──► AppRunner
│                                                  │
│                                                  └── вызывает ──► Features/<Hypothesis>
│
├── создаёт ────► CancellationTokenSource ◄── отменяет ── Console.CancelKeyPress
│                 │
│                 └── токен ──► AppRunner ──► Features/<Hypothesis>
│
└── ловит ──────► Exception / OperationCanceledException ──► ранний логгер
```

**Поток выполнения.** `Program.cs` создаёт ранний логгер, собирает host, резолвит `AppRunner` из DI, запускает сценарий в `Features/`, перехватывает необработанные исключения двумя блоками `catch`, устанавливает exit code, в `finally` останавливает host.

---

## Соглашения кода

### Двуязычные комментарии

Комментарии в коде — на русском и английском, разделены через `/`.

```csharp
// Prevent immediate process kill / Не даём процессу умереть сразу
e.Cancel = true;
```

### Секционные комментарии

Логические блоки в файлах выделяются секциями с двойным разделителем:

```csharp
// ═══════════════════════════════════════════════════════════════════════════════
// Section name / Название секции
// ═══════════════════════════════════════════════════════════════════════════════
```

Вложенные секции внутри `try` используют одинарный разделитель:

```csharp
// ─── Build host / Сборка хоста ───
```

### Двуязычные лог-сообщения

Runtime-сообщения логирования также двуязычны, разделены через `/`:

```csharp
logger.LogInformation("Готово / Done");
logger.LogError(ex, "Непредвиденная ошибка / Unhandled error");
```

### Маркеры в коде

В местах, куда предполагается писать логику гипотезы, оставлен маркер:

```csharp
// ─── Здесь код гипотезы / Hypothesis code goes here ───
```

---

## Обработка ошибок

Бизнес-ошибки в блоки `catch` не поступают: в minimal бизнес-слой отсутствует. До `Program.cs` доходят только инфраструктурные и необработанные исключения.

| Ситуация                     | Exit code | Уровень     | Логируется                  |
| ---------------------------- | --------- | ----------- | --------------------------- |
| Успешное завершение          | 0         | Information | «Готово / Done»             |
| `OperationCanceledException` | 130       | Information | «Прервано пользователем»    |
| Прочие исключения            | 1         | Error       | `ex.ToString()` (стектрейс) |

**Требования к реализации.**

- `catch (OperationCanceledException)` — **первым**, без стектрейса. Иначе `catch (Exception)` перехватит его, потому что `OperationCanceledException` наследуется от `SystemException`.
- `catch (Exception ex)` — вторым, `ex.ToString()` полностью. Единственное место в шаблоне, где пишется стектрейс.
- `finally` — `StopAsync` и `Dispose`. Оба обёрнуты в `if (host is not null)`, потому что `try` покрывает инициализацию, и хост мог не собраться. `StopAsync` — во внутреннем `try/catch`: исключение в `finally` внешнего `try` не перехватывается `catch`-блоками и убивает процесс.
- `return Environment.ExitCode;` в конце `Main` — **обязательно**. Без этого top-level вернёт 0 и перезапишет установленный код.

---

## Логирование

- `Microsoft.Extensions.Logging` с `AddSimpleConsole`.
- Параметры: `TimestampFormat = "HH:mm:ss.fff "`, `SingleLine = true`, `IncludeScopes = true`, цвет по уровню — по умолчанию.
- Уровень по умолчанию — `Information`.
- Serilog, JSON-формат и кастомный formatter не подключаются.

### Две логгер-фабрики

В `Program.cs` работают две независимые фабрики:

1. **Ранняя** (`LoggerFactory.Create`) — создаётся до `try`, используется самим `Program.cs`. Нужна потому, что `try` покрывает инициализацию хоста, и в `catch` должен быть куда писать.
2. **Хостовая** — создаётся внутри `builder.Build()`, используется `AppRunner` и фичами через `ILogger<T>`.

**Это осознанное решение.** Цена — три строки и одна локальная функция `ConfigureLogging`, общая на обе фабрики. Выигрыш — `try` покрывает всё, включая сборку хоста.

Обе фабрики пишут в `Console.Out`. Записи в консоль потокобезопасны по строке, interleaving практически невозможен.

### Логирование в finally

В `finally` пишется `"Остановка хоста / Stopping host"` перед `StopAsync` и `"Ошибка остановки хоста / Host stop failed"` — при исключении от `StopAsync`. Второе — во внутреннем `try/catch`, чтобы исключение остановки не перезаписало `Environment.ExitCode`.

---

## Отмена

Источник отмены — событие `Console.CancelKeyPress`:

- подписка на событие;
- `e.Cancel = true` для корректной остановки хоста;
- отмена `CancellationTokenSource`;
- токен передаётся в `AppRunner` и далее в сценарий.

`OperationCanceledException` распространяется через стек и перехватывается первым блоком `catch`. Обработка `SIGTERM` через `PosixSignalRegistration` не входит.

---

## `Directory.Build.props`

- `Nullable = enable`
- `ImplicitUsings = enable`
- `LangVersion = latest`
- `TreatWarningsAsErrors = true`
- `InvariantGlobalization = true`

Перечисленные настройки вводятся на старте: их добавление на поздних этапах разработки сопряжено с переработкой существующего кода.

**Важно.** Файл лежит **внутри** `faf-console-minimal/`, рядом с `.csproj`. Если положить его выше по дереву, он не попадёт в сгенерированный проект, и песочница останется без настроек.

---

## Область исключений

| Компонент                             | Обоснование                                        |
| ------------------------------------- | -------------------------------------------------- |
| `Result<T>`                           | В minimal бизнес-слой отсутствует                  |
| `ExpectedException`                   | Не требуется при использовании `Result<T>`         |
| `CancellationToken` из хоста          | Достаточно `CancelKeyPress`                        |
| `appsettings.json`                    | Настройка логирования выполняется в коде           |
| `appsettings.Production.json`         | Производственная среда отсутствует                 |
| Кастомный `ConsoleFormatter`          | `AddSimpleConsole` покрывает основные сценарии     |
| `TimeProvider`                        | Требуется только при работе со временем в сценарии |
| Serilog, JSON-логи                    | Избыточно для вывода в терминал                    |
| HTTP, `IHttpClientFactory`            | Не входит в minimal                                |
| MediatR, FluentValidation, AutoMapper | Относятся к прикладному уровню                     |
| Docker, CI, тесты по умолчанию        | Инфраструктура добавляется по факту необходимости  |

---

## Создание проекта из шаблона

### Установка

```bash
dotnet new install <path-to-template>/faf-console-minimal
```

### Проверка

```bash
dotnet new list | findstr faf
```

### Генерация

```bash
cd <sandbox-dir>
dotnet new faf-console-minimal -n MyHypothesis --dry-run
```

`--dry-run` — **обязательно** перед первой генерацией. Показывает список файлов и подстановок, ничего не пишет на диск. Позволяет поймать забытые `Faf.Console.Minimal` в коде.

Если `--dry-run` показал корректные подстановки:

```bash
dotnet new faf-console-minimal -n MyHypothesis
```

Создаст папку `MyHypothesis/` с проектом. Имя namespace, `.csproj` и корневой namespace внутри файлов станут `MyHypothesis`.

---

## Разработка шаблона

Движок шаблонов **не подхватывает изменения на лету**. При правке содержимого шаблона — полный цикл:

```bash
dotnet new uninstall faf-console-minimal
:: ...правки в faf-console-minimal/...
dotnet new install <path-to-template>/faf-console-minimal
dotnet new faf-console-minimal -n Scratch --dry-run
```

**Проверка после генерации.** Все вхождения `Faf.Console.Minimal` должны замениться на `-n`: имя `.csproj`, `namespace` в каждом `.cs`, `RootNamespace` в `.csproj`, имя профиля в `launchSettings.json`.

---

## Принципы

- Проект готов к написанию бизнес-логики непосредственно после создания из шаблона.
- Host используется как источник DI и `ILogger` без дополнительной обвязки.
- Обработка ошибок: инфраструктурные — через `catch (Exception)` верхнего уровня.
- Стектрейс логируется однократно — для необработанных исключений.
- Дополнительные компоненты вводятся по мере необходимости, а не заранее.

---
