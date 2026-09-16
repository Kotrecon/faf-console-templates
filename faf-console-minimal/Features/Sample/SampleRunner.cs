using Microsoft.Extensions.Logging;

namespace Faf.Console.Minimal.Features.Sample;

// ═══════════════════════════════════════════════════════════════════════════════
// Sample feature / Пример фичи
// Заглушка: существует, чтобы папка Features/ попала в git.
// В реальной гипотезе — заменить или удалить.
// Placeholder: exists so Features/ survives in git.
// In a real hypothesis — replace or delete.
// ═══════════════════════════════════════════════════════════════════════════════
internal sealed class SampleRunner
{
    private readonly ILogger<SampleRunner> _logger;

    public SampleRunner(ILogger<SampleRunner> logger)
    {
        _logger = logger;
    }

    public Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sample feature / Пример фичи");

        return Task.CompletedTask;
    }
}