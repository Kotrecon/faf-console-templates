# Roadmap — FAF Console Templates

План развития консольных шаблонов. Документ живой: пункты добавляются по мере
того, как упираемся в реальные потребности, а не заранее.

Принцип: **следующий уровень делается тогда, когда minimal перестаёт хватать**.
Не раньше. Иначе шаблон превращается в мини-фреймворк, который дольше настраивать,
чем проверять гипотезу.

---

## Уровни

Три уровня зрелости шаблона:

| Уровень   | Когда нужен                                                               | Объём          |
| --------- | ------------------------------------------------------------------------- | -------------- |
| `minimal` | Проверка гипотезы на 2–10 классах, одна папка, без инфраструктуры         | Готов          |
| `medium`  | Гипотеза подтвердилась, код живёт дольше одной сессии, появляются сервисы | По потребности |
| `adult`   | Прототип превращается в инструмент, который запускают другие              | По потребности |

Отдельная ось — **HTTP**. Не уровень, а специализация: `minimal-http`,
`medium-http`. Нужна, когда гипотеза связана с сетью, API, внешними сервисами.

---

## Уровень `minimal` — готов

Пакет: `Fafp.Templates.Console.Minimal@1.0.1`
Короткое имя: `faf-console-minimal`

Что внутри:

- `Host.CreateApplicationBuilder` как источник DI и `ILogger`
- `AddSimpleConsole` с коротким timestamp, single-line, scopes
- Две логгер-фабрики: ранняя (для `Program.cs`) и хостовая (для `AppRunner` и фич)
- `try / catch / finally` покрывает всю инициализацию, включая сборку хоста
- `catch (OperationCanceledException)` первым — exit code 130, без стектрейса
- `catch (Exception ex)` вторым — `ex.ToString()`, exit code 1
- `Console.CancelKeyPress` с `e.Cancel = true` — корректная остановка хоста
- `Environment.ExitCode` + `return Environment.ExitCode;` в конце `Main`
- `Directory.Build.props`: nullable, warnings-as-errors, LangVersion latest,
  InvariantGlobalization
- `Features/<Hypothesis>/` — одна папка на сценарий
- Bilingual comments и log messages (рус / англ)

Что сознательно **не** входит — см. README шаблона, раздел «Out of Scope».

---

## Уровень `medium` — по потребности

Триггеры перехода. Если хотя бы один срабатывает — пора:

- бизнес-логика возвращает не только успех/исключение, а разные исходы
- настройки перестали помещаться в код, нужны `appsettings.json`
- логирование `AddSimpleConsole` перестало устраивать по формату
- в сценарии появилось время (`DateTime.Now`, `Stopwatch`), которое хочется
  подменять в тестах
- сценарий запускается дольше нескольких секунд, нужна нормальная отмена
- появилось несколько сценариев, хочется выбирать по аргументу

Что добавляется:

### `Result<T>`

Возврат исходов без исключений. Отдельный проект или nuget-зависимость —
своя библиотека `Fafp.ResultPattern` (уже опубликована на nuget.org).

Форма:

- бизнес-слой возвращает `Result<T>`, исключений не бросает
- `Program.cs` умеет отображать `Result` в exit code
- маппинг «исход → код → уровень лога» фиксируется в таблице

Обработка ошибок в `Program.cs` меняется: `Result` обрабатывается до `catch`,
`catch` остаётся только для инфраструктуры и неожиданного.

### `appsettings.json`

- `appsettings.json` в корне проекта
- `appsettings.Development.json` — для локальной отладки
- `appsettings.Production.json` — **не создаётся**, пока нет продакшена
- секция `Logging` управляет уровнями
- переопределение через env vars (`Logging__LogLevel__Default=Debug`)

### Кастомный `ConsoleFormatter`

Когда `AddSimpleConsole` перестал устраивать по формату:

- свой `ConsoleFormatter` с полным контролем вывода
- регистрация через `AddConsoleFormatter<T, TOptions>` + `FormatterName`
- `ILogger<T>` как фасад сохраняется, меняется только рендер
- цвет по уровню — через ANSI-коды с проверкой, что вывод в терминал (не в файл)

### `TimeProvider`

- `builder.Services.AddSingleton(TimeProvider.System)`
- в коде — `TimeProvider.GetUtcNow()` вместо `DateTime.UtcNow`
- тесты могут подменять время без ожидания

### `CancellationToken` из `IHostApplicationLifetime`

- `IHostApplicationLifetime.ApplicationStopping` как источник отмены
- `Console.CancelKeyPress` остаётся для интерактивного Ctrl+C
- `PosixSignalRegistration` для `SIGTERM` — если понадобится запуск под
  systemd или в Docker

### Выбор сценария по аргументу

- `args[0]` — имя сценария
- простая маршрутизация через `switch` (без `System.CommandLine`)
- список доступных сценариев — из DI

### Конфигурация сред

- `DOTNET_ENVIRONMENT` управляет загрузкой `appsettings.{Env}.json`
- `launchSettings.json` содержит профили для Development и, при необходимости,
  для других сред

### Структура

- если фич стало больше трёх — возможно, `Features/<Name>/` с подпапками
  (`Models/`, `Services/`)
- если сценариев несколько — `AppRunner` маршрутизирует по имени

---

## Уровень `adult` — по потребности

Триггеры перехода:

- прототип превращается в инструмент, которым пользуются другие
- нужна диагностика в проде: логи в файл/коллектор, трейсы, метрики
- конфигурация стала сложной, появились секции, требующие типизации
- несколько независимых сценариев запускаются по расписанию или из CI

Что добавляется:

### Serilog или OpenTelemetry

- **Serilog** — если нужны structured logs в файл, Seq, Elastic и т. п.
  с минимальной настройкой
- **OpenTelemetry** — если нужны трейсы и метрики, экспорт в OTLP-коллектор
- `AddSimpleConsole` либо заменяется, либо остаётся как один из sinks

### `IOptions<T>`

- типизированные секции конфигурации
- валидация через `IValidateOptions<T>` на старте
- `AddOptions<T>().Bind(...).ValidateDataAnnotations().ValidateOnStart()`

### HTTP-ветка

См. отдельный раздел ниже.

### Публикация / упаковка

- если из песочницы получается исполняемый инструмент — `dotnet publish`
  с `PublishSingleFile`, `SelfContained`, обрезкой
- для внутреннего распространения — как nuget tool (`PackAsTool=true`)

### Наблюдаемость

- health checks через `Microsoft.Extensions.Diagnostics.HealthChecks`
- метрики через `System.Diagnostics.Metrics`
- корреляция логов и трейсов через `Activity.Current`

---

## Ось `-http` — отдельная специализация

Независимая от уровня: `minimal-http`, `medium-http`. Нужна, когда гипотеза
связана с сетью, внешними API или веб-сервисами.

### Пакеты

- `Microsoft.Extensions.Http` — `IHttpClientFactory`
- `Microsoft.Extensions.Http.Resilience` — ретраи, таймауты, circuit breaker

### Содержимое

#### `IHttpClientFactory`

- регистрация в DI
- управление `HttpClient`-ами через фабрику, без `new HttpClient()`
- `HttpClient` не дизпосится вручную, фабрика сама управляет временем жизни

#### Типизированный клиент

- `ApiClient` как класс с `HttpClient` в конструкторе
- один клиент — один внешний сервис
- `BaseAddress`, `Timeout`, `DefaultRequestHeaders` — на этапе регистрации

#### Механизмы устойчивости

- `AddStandardResilienceHandler()` для типовых ретраев
- или `AddResilienceHandler()` для кастомной политики:
  - retry с экспоненциальным backoff
  - timeout на попытку и на весь вызов
  - circuit breaker
  - bulkhead (ограничение параллельных вызовов)

#### Логирование HTTP

- `AddHttpClient` + `ILogger` пишет запросы и ответы на уровне Debug
- на Info — только ошибки и ретраи
- `HttpRequestMessage` и `HttpResponseMessage` — не логировать целиком,
  только метод, URL, статус, длительность

#### Тестовый сервер для проверки

- `WireMock.Net` или `MockHttp` для локальной проверки без реального сервиса
- поднимается из `Features/<Hypothesis>/` при запуске сценария

#### Структура

```

Features/
└── <Hypothesis>/
├── ApiClient.cs # типизированный клиент
├── ApiModels.cs # DTO
├── ApiClientExtensions.cs # регистрация в DI (AddHttpClient<ApiClient>)
└── <Runner>.cs # вызов клиента

```

#### Конфигурация

- `appsettings.json` содержит секцию с `BaseAddress`, `Timeout`, ключами
- секреты — через user-secrets в Development, env vars в остальном
- не коммитить токены и ключи

---

## Что откладывается до реального запроса

Не делаем, пока не упрёмся. Просто держим в голове:

- **gRPC** — если понадобится, отдельная ветка `-grpc` по аналогии с `-http`
- **WebSocket / SignalR** — если гипотеза про real-time
- **EF Core / Dapper** — если нужна БД, отдельная ветка `-data`
- **Message queues** (RabbitMQ, Kafka, Azure Service Bus) — `-mq`
- **`System.CommandLine`** — если сценариев станет много и `switch` по `args[0]`
  перестанет справляться
- **Тесты по умолчанию** — если шаблон используется для чего-то, что живёт
  дольше песочницы
- **CI/CD** — если шаблон начнут устанавливать из пайплайнов
- **Docker** — если сценарий поедет в контейнер

Правило: **не тащить в шаблон то, что ещё не понадобилось дважды**.

---

## Соглашения по именованию

При добавлении сиблингов:

| Что               | Значение                                  | Стиль      | Пример                               |
| ----------------- | ----------------------------------------- | ---------- | ------------------------------------ |
| Папка шаблона     | `<shortName>`                             | kebab-case | `faf-console-minimal`                |
| `shortName`       | `faf-console-<level>[-<axis>]`            | kebab-case | `faf-console-medium-http`            |
| `sourceName`      | `Faf.Console.<Level>[.<Axis>]`            | PascalCase | `Faf.Console.Medium.Http`            |
| `identity`        | совпадает с `sourceName`                  | PascalCase | `Faf.Console.Medium.Http`            |
| Файл проекта      | `<sourceName>.csproj`                     | PascalCase | `Faf.Console.Medium.Http.csproj`     |
| NuGet `PackageId` | `Fafp.Templates.Console.<Level>[.<Axis>]` | PascalCase | `Fafp.Templates.Console.Medium.Http` |

Префикс `Fafp.` — обязателен для nuget-пакетов (префикс `Faf` зарезервирован
другим владельцем).

---

## Версионирование

- `minimal` — `1.x.x`
- `medium` — `1.x.x` (независимая линейка)
- `adult` — `1.x.x` (независимая линейка)
- `-http`-варианты — свои линейки, версии не связаны с базовыми

При изменении шаблона:

- правки документации/метаданных — patch (`1.0.0` → `1.0.1`)
- добавление компонентов, не ломающих сгенерированные проекты — minor (`1.1.0`)
- изменение структуры шаблона, ломающее ожидания — major (`2.0.0`)

---

## Процесс добавления нового шаблона

1. Скопировать `faf-console-minimal/` в новую папку с новым именем
2. Переименовать `.csproj` (рабочий и pack) по соглашению
3. Поправить `PackageId`, `PackageVersion`, `Title`, `Description`
4. Поправить `.template.config/template.json`: `identity`, `name`, `shortName`,
   `sourceName`
5. Поправить `RootNamespace` в рабочем `.csproj`
6. Прогнать `dotnet pack` → `dotnet new install` → `--dry-run` → проверить
   подстановки
7. Добавить строку в корневой `README.md` и `README.ru.md`
8. Опубликовать в nuget.org
9. Закоммитить в git

---

## Что уже сделано

- [x] `faf-console-minimal` — v1.0.1
- [x] Репозиторий `faf-console-templates` на GitHub
- [x] Публикация в nuget.org под префиксом `Fafp.Templates.Console.*`
- [x] Иконка пакета, ASCII-схема в README, MIT-лицензия

## Что в работе

- [ ] —

## Что запланировано

- [ ] `faf-console-medium` — по факту упора в ограничения minimal
- [ ] `faf-console-minimal-http` — при первой сетевой гипотезе
- [ ] `faf-console-medium-http` — при первой сетевой гипотезе средней сложности

---
