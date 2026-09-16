# Faf.Console.Minimal — Minimal Console Boilerplate

Console application template for testing hypotheses on 2–10 classes. One scenario — one folder.

---

## Purpose

The template is designed for testing technical hypotheses in a console application. Target size — two to ten classes. One scenario is placed in one folder.

---

## Scope Boundary

The template ends where `Result<T>`, `CancellationToken` from the host, `appsettings`, custom log formatters, HTTP, and other infrastructure begin.

---

## File Structure

```bash
/
├── README.md                      # English versionRussian version
├── README.ru.md                   # Russian version
├── Program.cs                     # orchestration: host, try/catch/finally, exit code
├── AppRunner.cs                   # scenario entry point, resolved from DI
├── Features/
│   └── <Hypothesis>/              # one scenario — one folder
│       ├── <scenario classes>
│       └── ...
├── Properties/
│   └── launchSettings.json        # DOTNET_ENVIRONMENT=Development
├── Directory.Build.props          # nullable, warnings-as-errors, LangVersion
└── <project>.csproj
```

**Placement rule.** All classes related to the scenario are placed in `Features/<Hypothesis>/`. Only `Program.cs`, `AppRunner.cs`, and build files remain at the root. Removing a hypothesis reduces to removing one folder.

Inside a feature, classes are placed flat while their count does not exceed 7–8. Beyond that — grouping by purpose within the same folder.

---

## Relationship Diagram

```bash
Program.cs (top-level)
│
├── creates ────► LoggerFactory (early logger)
│                 │
│                 └── used by Program.cs for catch logging
│
├── builds ─────► Host.CreateApplicationBuilder
│                 │
│                 ├── provides ──► ILogger (AddSimpleConsole)
│                 │
│                 └── provides ──► IServiceProvider
│                                    │
│                                    └── injects ──► AppRunner
│                                                     │
│                                                     └── invokes ──► Features/<Hypothesis>
│
├── creates ────► CancellationTokenSource ◄── cancels ── Console.CancelKeyPress
│                 │
│                 └── token ──► AppRunner ──► Features/<Hypothesis>
│
└── catches ────► Exception / OperationCanceledException ──► early logger
```

**Execution flow.** `Program.cs` creates the early logger, builds the host, resolves `AppRunner` from DI, runs the scenario in `Features/`, catches unhandled exceptions in two `catch` blocks, sets the exit code, and stops the host in `finally`.

---

## Code Conventions

### Bilingual Comments

Comments in code are written in Russian and English, separated by `/`.

```csharp
// Prevent immediate process kill / Не даём процессу умереть сразу
e.Cancel = true;
```

### Section Comments

Logical blocks in files are marked with double-divider sections:

```csharp
// ═══════════════════════════════════════════════════════════════════════════════
// Section name / Название секции
// ═══════════════════════════════════════════════════════════════════════════════
```

Nested sections inside `try` use a single divider:

```csharp
// ─── Build host / Сборка хоста ───
```

### Bilingual Log Messages

Runtime log messages are also bilingual, separated by `/`:

```csharp
logger.LogInformation("Готово / Done");
logger.LogError(ex, "Непредвиденная ошибка / Unhandled error");
```

### Code Markers

A marker is left where hypothesis logic is expected to be written:

```csharp
// ─── Здесь код гипотезы / Hypothesis code goes here ───
```

---

## Error Handling

Business errors do not reach `catch` blocks: in minimal there is no business layer. Only infrastructure and unhandled exceptions reach `Program.cs`.

| Situation                    | Exit code | Level       | Logged                       |
| ---------------------------- | --------- | ----------- | ---------------------------- |
| Successful completion        | 0         | Information | "Готово / Done"              |
| `OperationCanceledException` | 130       | Information | "Прервано пользователем"     |
| Other exceptions             | 1         | Error       | `ex.ToString()` (stacktrace) |

**Implementation requirements.**

- `catch (OperationCanceledException)` — **first**, without stacktrace. Otherwise `catch (Exception)` would intercept it, since `OperationCanceledException` derives from `SystemException`.
- `catch (Exception ex)` — second, `ex.ToString()` in full. The only place in the template where a stacktrace is written.
- `finally` — `StopAsync` and `Dispose`. Both wrapped in `if (host is not null)`, because `try` covers initialization and the host may not have been built. `StopAsync` — inside an inner `try/catch`: an exception in the `finally` of the outer `try` is not caught by the `catch` blocks and kills the process.
- `return Environment.ExitCode;` at the end of `Main` — **mandatory**. Without it, top-level returns 0 and overwrites the set code.

---

## Logging

- `Microsoft.Extensions.Logging` with `AddSimpleConsole`.
- Parameters: `TimestampFormat = "HH:mm:ss.fff "`, `SingleLine = true`, `IncludeScopes = true`, level color — default.
- Default level — `Information`.
- Serilog, JSON format, and custom formatter are not included.

### Two Logger Factories

`Program.cs` uses two independent factories:

1. **Early** (`LoggerFactory.Create`) — created before `try`, used by `Program.cs` itself. Needed because `try` covers host initialization, and `catch` must have somewhere to write.
2. **Host** — created inside `builder.Build()`, used by `AppRunner` and features via `ILogger<T>`.

**This is a deliberate decision.** Cost — three lines and one local `ConfigureLogging` function shared by both factories. Benefit — `try` covers everything, including host construction.

Both factories write to `Console.Out`. Console writes are line-thread-safe; interleaving is practically impossible.

### Logging in finally

In `finally`, `"Остановка хоста / Stopping host"` is written before `StopAsync` and `"Ошибка остановки хоста / Host stop failed"` — on exception from `StopAsync`. The latter is inside an inner `try/catch` so that a stop exception does not overwrite `Environment.ExitCode`.

---

## Cancellation

Cancellation source — the `Console.CancelKeyPress` event:

- subscribe to the event;
- `e.Cancel = true` for graceful host shutdown;
- cancel the `CancellationTokenSource`;
- the token is passed to `AppRunner` and further to the scenario.

`OperationCanceledException` propagates through the stack and is caught by the first `catch` block. `SIGTERM` handling via `PosixSignalRegistration` is not included.

---

## `Directory.Build.props`

- `Nullable = enable`
- `ImplicitUsings = enable`
- `LangVersion = latest`
- `TreatWarningsAsErrors = true`
- `InvariantGlobalization = true`

These settings are introduced from the start: adding them later involves reworking existing code.

**Important.** The file is located **inside** `faf-console-minimal/`, next to the `.csproj`. If placed higher up the tree, it will not be copied into the generated project, and the sandbox will be left without settings.

---

## Out of Scope

| Component                             | Rationale                                        |
| ------------------------------------- | ------------------------------------------------ |
| `Result<T>`                           | No business layer in minimal                     |
| `ExpectedException`                   | Not needed when using `Result<T>`                |
| `CancellationToken` from host         | `CancelKeyPress` is sufficient                   |
| `appsettings.json`                    | Logging configuration is done in code            |
| `appsettings.Production.json`         | No production environment                        |
| Custom `ConsoleFormatter`             | `AddSimpleConsole` covers the main scenarios     |
| `TimeProvider`                        | Needed only when working with time in a scenario |
| Serilog, JSON logs                    | Redundant for terminal output                    |
| HTTP, `IHttpClientFactory`            | Not part of minimal                              |
| MediatR, FluentValidation, AutoMapper | Belong to the application layer                  |
| Docker, CI, tests by default          | Infrastructure is added as needed                |

---

## Creating a Project from the Template

### Install

```bash
dotnet new install <path-to-template>/faf-console-minimal
```

### Verify

```bash
dotnet new list | findstr faf
```

### Generate

```bash
cd <sandbox-dir>
dotnet new faf-console-minimal -n MyHypothesis --dry-run
```

`--dry-run` is **mandatory** before the first generation. It shows the file list and substitutions without writing anything to disk. Allows catching forgotten `Faf.Console.Minimal` occurrences in the code.

If `--dry-run` shows correct substitutions:

```bash
dotnet new faf-console-minimal -n MyHypothesis
```

Creates a `MyHypothesis/` folder with the project. The namespace, `.csproj`, and root namespace inside files become `MyHypothesis`.

---

## Template Development

The template engine **does not pick up changes on the fly**. When editing template contents — full cycle:

```bash
dotnet new uninstall faf-console-minimal
:: ...edits in faf-console-minimal/...
dotnet new install <path-to-template>/faf-console-minimal
dotnet new faf-console-minimal -n Scratch --dry-run
```

**Post-generation check.** All occurrences of `Faf.Console.Minimal` must be replaced with `-n`: the `.csproj` name, `namespace` in each `.cs`, `RootNamespace` in the `.csproj`, and the profile name in `launchSettings.json`.

---

## Principles

- The project is ready for writing business logic immediately after generation from the template.
- The host is used as a source of DI and `ILogger` without additional wiring.
- Error handling: infrastructure errors — via a top-level `catch (Exception)`.
- Stacktrace is logged exactly once — for unhandled exceptions.
- Additional components are introduced as needed, not in advance.

---
