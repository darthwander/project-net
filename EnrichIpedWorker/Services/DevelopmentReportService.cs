using CsvHelper;
using CsvHelper.Configuration;

using EnrichIped.BackgroundServices.Constants;
using EnrichIped.BackgroundServices.Services.Abstractions;
using EnrichIped.BackgroundServices.Services.Base;
using EnrichIped.Client.Abstractions;
using EnrichIped.Client.Configurations;
using EnrichIped.DataInfrastructure.Dtos.DevelopmentReport;
using EnrichIped.DataInfrastructure.Repositories.Abstractions;

using Serilog;

namespace EnrichIped.BackgroundServices.Services;

internal class DevelopmentReportService : BaseReportService, IDevelopmentReportService
{
	private readonly IServiceProvider _serviceProvider;
	private IIpedDevelopmentRepository? _repository;

	public DevelopmentReportService(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}

	public async Task ImportAsync(IpedSettings ipedSettings)
	{
		using var scope = _serviceProvider.CreateScope();
		var httpFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
		var ipedClient = scope.ServiceProvider.GetRequiredService<IIpedClient>();
		var repository = scope.ServiceProvider.GetRequiredService<IIpedDevelopmentRepository>();
		var configurationRepository = scope.ServiceProvider.GetRequiredService<IIpedConfigurationRepository>();

		var tokens = ipedSettings.GetAllTokens().ToList();
		Log.Logger.Information($"Iniciando importação para {tokens.Count} token(s): Development Report");

		foreach (var token in tokens)
		{
			Log.Logger.Information($"Processando token: {token[..Math.Min(3, token.Length)]}...");

			var settingsForToken = new IpedSettings
			{
				Uri = ipedSettings.Uri,
				Token = token
			};

			try
			{
				await ImportAsync(httpFactory, ipedClient, repository, configurationRepository, settingsForToken);
				Log.Logger.Information($"Token processado com sucesso: {token[..Math.Min(3, token.Length)]}...");
			}
			catch (Exception e)
			{
				Log.Logger.Error(e, $"Erro ao processar token {token[..Math.Min(3, token.Length)]}...: {e.Message}");
			}
		}

		Log.Logger.Information("Importação concluída para todos os tokens: Development Report");
	}

	public async Task ImportAsync(
		IHttpClientFactory httpFactory,
		IIpedClient ipedClient,
		IIpedDevelopmentRepository repository,
		IIpedConfigurationRepository configurationRepository,
		IpedSettings ipedSettings)
	{
		Http = httpFactory.CreateClient();
		_repository = repository;
		ConfigurationRepository = configurationRepository;

		CheckParameters(_repository, ipedSettings);
		long configId = 0;
		var lastExecutionResult = string.Empty;

		try
		{
			var start = DateTime.UtcNow;

			var (lastFileExpiresAt, configIdAux) =
				await GetConfigIdAndLastExpiresAtAsync(IpedConstants.DevelopmentServiceType);
			configId = configIdAux;

			while ((DateTime.UtcNow - start).TotalMinutes < TimeoutTotalMinutes)
			{
				var response = await GetReportAsync(IpedConstants.DevelopmentServiceType, ipedClient, ipedSettings);

				if (GetStatusAndExpiresAt(
						IpedConstants.DevelopmentServiceType,
						response,
						lastFileExpiresAt,
						out var expiresAt,
						ref lastExecutionResult,
						out var status))
					break;

				if (expiresAt > DateTime.UtcNow
					&& status == IpedConstants.CompletedReportStatus)
				{
					var (configIdInserted, sw) =
						await ExecuteAsync(
							ExecuteIpedDevelopmentReportAsync,
							IpedConstants.DevelopmentServiceType,
							response,
							expiresAt);

					if (configIdInserted > 0)
						configId = configIdInserted;

					lastExecutionResult =
						$"Relatório '{IpedConstants.DevelopmentServiceType}' processado em {sw.Elapsed}.";
					Log.Logger.Information(lastExecutionResult);
					break;
				}

				lastExecutionResult =
					$"Relatório '{IpedConstants.DevelopmentServiceType}' com status ainda '{status ?? "desconhecido"}'. Aguardando {DelayMinutes} minutos...";
				await ConfigurationRepository.SetLastExecutionAsync(configId, lastExecutionResult, DateTime.UtcNow);

				Log.Logger.Warning(lastExecutionResult);
				await Task.Delay(TimeSpan.FromMinutes(DelayMinutes));
			}
		}
		catch (Exception e)
		{
			lastExecutionResult = $"Erro ao buscar o relatório '{IpedConstants.DevelopmentServiceType}'.";
			Log.Logger.Error(e, lastExecutionResult);
		}
		finally
		{
			if (configId > 0)
				await ConfigurationRepository.SetLastExecutionAsync(configId, lastExecutionResult, DateTime.UtcNow);

			_repository?.Dispose();
			ConfigurationRepository?.Dispose();
		}
	}

	private async Task ExecuteIpedDevelopmentReportAsync(long configId, byte[] bytes)
	{
        var encoding = DetectEncoding(bytes);
        using var stream = new MemoryStream(bytes);
        var sr = new StreamReader(stream, encoding);

        var config = CsvConfiguration.FromAttributes<DevelopmentReportDto>();
		config.Delimiter = DefaultDelimiter;
		config.HasHeaderRecord = true;
		config.BadDataFound = null;
		config.MissingFieldFound = null;
		config.HeaderValidated = null;
		config.IgnoreBlankLines = true;
		config.Mode = CsvMode.RFC4180;
		config.AllowComments = true;
		config.TrimOptions = TrimOptions.Trim;
		using var csv = new CsvReader(sr, config);
		var list = csv.GetRecords<DevelopmentReportDto>().ToList();

		if (list.Count < 1)
		{
			var message = $"Table not found in the report '{IpedConstants.DevelopmentServiceType}'.";
			Log.Logger.Warning(message);
			await ConfigurationRepository!.SetLastExecutionAsync(configId, message, DateTime.UtcNow);
			return;
		}

		var (lastCompletedSyncResult, success) = await _repository!.SaveDevelopmentReportAsync(list);

		Log.Logger.Information(
			$"Processamento do Relatório '{IpedConstants.DevelopmentServiceType}' concluído: '{lastCompletedSyncResult}'");

		if (success)
			await ConfigurationRepository!.SetLastSyncAsync(configId, lastCompletedSyncResult, DateTime.UtcNow);
		else
			await ConfigurationRepository!.SetLastExecutionAsync(configId, lastCompletedSyncResult, DateTime.UtcNow);

		await _repository.RemoveDuplicatedDevelopmentReportAsync();
	}
}