# FAF Console Templates

Консольные шаблоны приложений для проверки гипотез и прототипов.
Распространяются через NuGet под префиксом `Fafp.Templates.Console.*`
и устанавливаются командой `dotnet new install`.

## Шаблоны

| Пакет                            | Короткое имя          | Описание                                                          |
| -------------------------------- | --------------------- | ----------------------------------------------------------------- |
| `Fafp.Templates.Console.Minimal` | `faf-console-minimal` | Минимальный консольный шаблон для проверки гипотез (2–10 классов) |

## Установка

```bash
dotnet new install Fafp.Templates.Console.Minimal
```

## Использование

```bash
dotnet new faf-console-minimal -n MyHypothesis -o MyHypothesis
cd MyHypothesis
dotnet run
```

## Структура репозитория

```bash
.
├── README.md
├── README.ru.md
├── LICENSE
├── .gitignore
└── faf-console-minimal/
    ├── .template.config/
    ├── Features/
    ├── Properties/
    ├── Program.cs
    ├── AppRunner.cs
    ├── Faf.Console.Minimal.csproj
    ├── Faf.Console.Minimal.Template.csproj
    ├── Directory.Build.props
    ├── README.md
    ├── README.ru.md
    └── icon_128x128.png
```

## Лицензия

MIT

---
