# FAF Console Templates

Console application templates for hypothesis testing and prototypes.
Distributed via NuGet under the `Fafp.Templates.Console.*` namespace
and installed with `dotnet new install`.

## Templates

| Package                          | Short Name            | Description                                                       |
| -------------------------------- | --------------------- | ----------------------------------------------------------------- |
| `Fafp.Templates.Console.Minimal` | `faf-console-minimal` | Minimal console boilerplate for hypothesis testing (2–10 classes) |

## Installation

```bash
dotnet new install Fafp.Templates.Console.Minimal
```

## Usage

```bash
dotnet new faf-console-minimal -n MyHypothesis -o MyHypothesis
cd MyHypothesis
dotnet run
```

## Repository Layout

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

## License

MIT

---
