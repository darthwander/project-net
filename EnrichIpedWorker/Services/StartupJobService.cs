using EnrichIped.BackgroundServices.Services.Abstractions;
using EnrichIped.Client.Configurations;

using Hangfire;

using Serilog;

namespace EnrichIped.BackgroundServices.Services;

internal class StartupJobService : IHostedService
{
	private readonly IConfiguration _configuration;

	public StartupJobService(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		await Task.Delay(TimeSpan.FromMinutes(1));
		Log.Logger.Information("Executando jobs de inicialização...");

		var ipedSettings = _configuration.GetSection(nameof(IpedSettings)).Get<IpedSettings>();

		if (ipedSettings is null)
			return;

		//BackgroundJob.Enqueue<ILogReportService>(service => service.ImportAsync(ipedSettings));
		BackgroundJob.Enqueue<IDevelopmentReportService>(service => service.ImportAsync(ipedSettings));
		//BackgroundJob.Enqueue<ICompleteReportService>(service => service.ImportAsync(ipedSettings));

		Log.Logger.Information("Job de inicialização enfileirado com sucesso.");
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}
}