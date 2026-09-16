using Faf.Console.Minimal;
using Faf.Console.Minimal.Features.Sample;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


// ═══════════════════════════════════════════════════════════════════════════════
// Logging configuration / Настройка логирования
// ═══════════════════════════════════════════════════════════════════════════════
static void ConfigureLogging(ILoggingBuilder logging)
{
    logging.ClearProviders();
    logging.AddSimpleConsole(options =>
    {
        options.TimestampFormat = "[HH:mm:ss.fff] ";
        options.SingleLine = true;
        options.IncludeScopes = true;
    });
}

// ═══════════════════════════════════════════════════════════════════════════════
// Early logger / Ранний логгер
// Доступен до сборки хоста — для ошибок инициализации.
// Available before host is built — for initialization errors.
// ═══════════════════════════════════════════════════════════════════════════════
using var earlyLoggerFactory = LoggerFactory.Create(ConfigureLogging);
var logger = earlyLoggerFactory.CreateLogger("Program");

// ═══════════════════════════════════════════════════════════════════════════════
// State / Состояние
// ═══════════════════════════════════════════════════════════════════════════════
IHost? host = null;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    // Prevent immediate process kill / Не даём процессу умереть сразу
    e.Cancel = true;
    cts.Cancel();
};

// ═══════════════════════════════════════════════════════════════════════════════
// Execution / Исполнение
// ═══════════════════════════════════════════════════════════════════════════════
try
{
    // ─── Build host / Сборка хоста ───
    var builder = Host.CreateApplicationBuilder(args);
    ConfigureLogging(builder.Logging);
    builder.Services.AddSingleton<AppRunner>();
    builder.Services.AddSingleton<SampleRunner>();
    host = builder.Build();

    // ─── Run scenario / Запуск сценария ───
    var runner = host.Services.GetRequiredService<AppRunner>();
    await runner.RunAsync(cts.Token);

    logger.LogInformation("Готово / Done");
}
catch (OperationCanceledException)
{
    logger.LogInformation("Прервано пользователем / Cancelled by user");
    Environment.ExitCode = 130;
}
catch (Exception ex)
{
    logger.LogError(ex, "Непредвиденная ошибка / Unhandled error");
    Environment.ExitCode = 1;
}
finally
{
    // ─── Shutdown / Остановка ───
    if (host is not null)
    {
        logger.LogInformation("Остановка хоста / Stopping host");
        try
        {
            await host.StopAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка остановки хоста / Host stop failed");
        }
        host.Dispose();
    }
}

return Environment.ExitCode;