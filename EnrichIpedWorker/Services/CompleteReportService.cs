using CsvHelper;
using CsvHelper.Configuration;

using EnrichIped.BackgroundServices.Constants;
using EnrichIped.BackgroundServices.Services.Abstractions;
using EnrichIped.BackgroundServices.Services.Base;
using EnrichIped.Client.Abstractions;
using EnrichIped.Client.Configurations;
using EnrichIped.DataInfrastructure.Dtos.CompleteReport;
using EnrichIped.DataInfrastructure.Repositories.Abstractions;

using Serilog;

namespace EnrichIped.BackgroundServices.Services;

internal class CompleteReportService : BaseReportService, ICompleteReportService
{
	private readonly IServiceProvider _serviceProvider;
	private IIpedCompleteRepository? _repository;

	public CompleteReportService(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}

	public async Task ImportAsync(
		IHttpClientFactory httpFactory,
		IIpedClient ipedClient,
		IIpedCompleteRepository repository,
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

			(var lastFileExpiresAt, configId) =
				await GetConfigIdAndLastExpiresAtAsync(IpedConstants.CompleteServiceType);

			while ((DateTime.UtcNow - start).TotalMinutes < TimeoutTotalMinutes)
			{
				var response = await GetReportAsync(IpedConstants.CompleteServiceType, ipedClient, ipedSettings);

				if (GetStatusAndExpiresAt(
						IpedConstants.CompleteServiceType,
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
							ExecuteIpedCompleteReportAsync,
							IpedConstants.CompleteServiceType,
							response,
							expiresAt);

					if (configIdInserted > 0)
						configId = configIdInserted;

					lastExecutionResult =
						$"Relatório '{IpedConstants.CompleteServiceType}' processado em {sw.Elapsed}.";
					Log.Logger.Information(lastExecutionResult);
					break;
				}

				lastExecutionResult =
					$"Relatório '{IpedConstants.CompleteServiceType}' com status ainda '{status ?? "desconhecido"}'. Aguardando {DelayMinutes} minutos...";
				await ConfigurationRepository.SetLastExecutionAsync(configId, lastExecutionResult, DateTime.UtcNow);

				Log.Logger.Warning(lastExecutionResult);
				await Task.Delay(TimeSpan.FromMinutes(DelayMinutes));
			}
		}
		catch (Exception e)
		{
			lastExecutionResult = $"Erro ao buscar o relatório '{IpedConstants.CompleteServiceType}'.";
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

	private async Task ExecuteIpedCompleteReportAsync(long configId, byte[] bytes)
	{
        var encoding = DetectEncoding(bytes);
        using var stream = new MemoryStream(bytes);
        var sr = new StreamReader(stream, encoding);

        var config = CsvConfiguration.FromAttributes<CompleteReportDto>();
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
		var list = csv.GetRecords<CompleteReportDto>().ToList();

		if (list.Count < 1)
		{
			Log.Logger.Warning($"Table not found in the report '{IpedConstants.CompleteServiceType}'.");
			return;
		}

		var (lastCompletedSyncResult, success) = await _repository!.SaveCompleteReportAsync(list);

		Log.Logger.Information(
			$"Processamento do Relatório '{IpedConstants.CompleteServiceType}' concluído: '{lastCompletedSyncResult}'");

		if (success)
			await ConfigurationRepository!.SetLastSyncAsync(configId, lastCompletedSyncResult, DateTime.UtcNow);
		else
			await ConfigurationRepository!.SetLastExecutionAsync(configId, lastCompletedSyncResult, DateTime.UtcNow);

		await _repository.RemoveDuplicatedCompleteReportAsync();
	}

	public async Task ImportAsync(IpedSettings ipedSettings)
	{
		using var scope = _serviceProvider.CreateScope();
		var httpFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
		var ipedClient = scope.ServiceProvider.GetRequiredService<IIpedClient>();
		var repository = scope.ServiceProvider.GetRequiredService<IIpedCompleteRepository>();
		var configurationRepository = scope.ServiceProvider.GetRequiredService<IIpedConfigurationRepository>();

		var tokens = ipedSettings.GetAllTokens().ToList();
		Log.Logger.Information($"Iniciando importação para {tokens.Count} token(s): Complete Report");

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

		Log.Logger.Information("Importação concluída para todos os tokens: Complete Report");
	}
}