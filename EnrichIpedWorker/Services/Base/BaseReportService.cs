using EnrichIped.Client.Abstractions;
using EnrichIped.Client.Configurations;
using EnrichIped.Client.Models.Responses.Reports;
using EnrichIped.DataInfrastructure.Repositories.Abstractions;

using Refit;

using Serilog;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace EnrichIped.BackgroundServices.Services.Base;

internal abstract class BaseReportService
{
	protected const string DefaultDelimiter = ";";
	protected const int DelayMinutes = 5;
	protected const int TimeoutTotalMinutes = 60;
	protected HttpClient? Http;
	protected IIpedConfigurationRepository? ConfigurationRepository;

	protected async Task<(DateTime lastFileExpiresAt, long configId)> GetConfigIdAndLastExpiresAtAsync(string type)
	{
		var (configIdAux, lastFileExpiresAt) =
			await ConfigurationRepository!.GetLastFileExpiresAtAsync(type);

		lastFileExpiresAt ??= DateTime.UtcNow.AddDays(-1);
		configIdAux = configIdAux > 0 ? configIdAux : 0;

		return (lastFileExpiresAt.Value, configIdAux);
	}

	protected static bool GetStatusAndExpiresAt(
		string type,
		ApiResponse<ReportResponse> response,
		[DisallowNull] DateTime? lastFileExpiresAt,
		out DateTime expiresAt,
		ref string lastExecutionResult,
		out string? status)
	{
		_ = DateTime.TryParse(response.Content?.Report?.ExpiresAt, out expiresAt);

		if (expiresAt == lastFileExpiresAt)
		{
			lastExecutionResult =
				$"Relatório '{type}' não alterado. Aguardando próxima execução.";
			Log.Logger.Information(lastExecutionResult);
			status = null;
			return true;
		}

		status = response.Content?.Report?.Status?.ToLowerInvariant();
		return false;
	}

	protected static async Task<ApiResponse<ReportResponse>> GetReportAsync(
		string type,
		IIpedClient ipedClient,
		IpedSettings ipedSettings)
	{
		var response =
			await ipedClient.GetReportAsync(ipedSettings.Token!, type);

		if (response.IsSuccessStatusCode) return response;

		Log.Logger.Fatal(
			$"Erro ao buscar o relatório '{type}'.");
		throw response.Error;
	}

	protected async Task<(long, Stopwatch)> ExecuteAsync(
		Func<long, byte[], Task> methodToExecute,
		string type,
		ApiResponse<ReportResponse> response,
		DateTime expiresAt)
	{
		var sw = new Stopwatch();
		sw.Start();
		Log.Logger.Information("Relatório pronto, iniciando processamento...");

		var configId = await ConfigurationRepository!.InsertAsync(
			type
			, response.Content?.Report?.File
			, expiresAt);

		Http!.DefaultRequestHeaders.Add("Accept", "text/csv;charset=UTF-8");
		Http.DefaultRequestHeaders.Add("Accept-Charset", "UTF8");
		var fileBytes = await Http.GetByteArrayAsync(response.Content?.Report?.File);

		await methodToExecute(configId, fileBytes);
		sw.Stop();
		return (configId, sw);
	}

	protected void CheckParameters(object? repository, IpedSettings? ipedSettings)
	{
		if (repository is null)
		{
			const string message = "Repository not found.";
			Log.Logger.Fatal(message);
			throw new Exception(message);
		}

		if (ConfigurationRepository is null)
		{
			const string message = "ConfigurationRepository not found.";
			Log.Logger.Fatal(message);
			throw new Exception(message);
		}

		if (ipedSettings is not null && !string.IsNullOrWhiteSpace(ipedSettings.Token)) return;
		{
			const string message = "IpedSettings not found.";
			Log.Logger.Fatal(message);
			throw new Exception(message);
		}
	}

    protected static Encoding DetectEncoding(byte[] bytes)
    {
        if (bytes is [0xEF, 0xBB, 0xBF, ..]) return Encoding.UTF8;

        if (bytes is [0xFF, 0xFE, ..]) return Encoding.Unicode;

        if (bytes is [0xFE, 0xFF, ..]) return Encoding.BigEndianUnicode;

        try
        {
            return Encoding.GetEncoding(1252);
        }
        catch
        {
            return Encoding.GetEncoding("ISO-8859-1");
        }
    }
}