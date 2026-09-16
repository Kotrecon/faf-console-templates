using Faf.Console.Minimal.Features.Sample;
using Microsoft.Extensions.Logging;

namespace Faf.Console.Minimal;

// ═══════════════════════════════════════════════════════════════════════════════
// Scenario entry point / Точка входа в сценарий
// Вся логика гипотезы — здесь или в Features/<Name>/.
// All hypothesis logic lives here or in Features/<Name>/.
// ═══════════════════════════════════════════════════════════════════════════════
internal sealed class AppRunner
{
    private readonly ILogger<AppRunner> _logger;
    private readonly SampleRunner _sampleRunner;

    public AppRunner(ILogger<AppRunner> logger, SampleRunner sampleRunner)
    {
        _logger = logger;
        _sampleRunner = sampleRunner;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Сценарий запущен / Scenario started");

        // ─── Здесь код гипотезы / Hypothesis code goes here ───
        await _sampleRunner.RunAsync(cancellationToken);

    }
}