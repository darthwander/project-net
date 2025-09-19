using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace EnrichIped.BackgroundServices.Configurations;

internal static class LogConfiguration
{
	private const string ApplicationName = "Refuturiza.Iped.Worker";

	public static IServiceCollection AddLogConfiguration(this IServiceCollection services, IConfiguration configuration)
	{
		Log.Logger = new LoggerConfiguration()
			.ReadFrom.Configuration(configuration)
			.Enrich.FromLogContext()
			.Enrich.WithMachineName()
			.Enrich.WithEnvironmentName()
			.Enrich.WithThreadId()
			.Enrich.WithProcessId()
			.Enrich.WithProperty("ApplicationName", ApplicationName)
			.WriteTo.Console(
				outputTemplate:
				"[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Properties:j}{NewLine}{Exception}",
				theme: AnsiConsoleTheme.Code)
			.CreateBootstrapLogger();

		Log.Logger.Information("Inicializando {ApplicationName}", ApplicationName);
		Log.Logger.Information("Logger configurado com sucesso.");

		return services;
	}
}