using Dapper;

using EnrichIped.DataInfrastructure.Configurations;
using EnrichIped.DataInfrastructure.Constants;
using EnrichIped.DataInfrastructure.Dtos.DevelopmentReport;
using EnrichIped.DataInfrastructure.Extensions;
using EnrichIped.DataInfrastructure.Queries;
using EnrichIped.DataInfrastructure.Repositories.Abstractions;

using Microsoft.Extensions.Configuration;

using MySqlConnector;

using Serilog;

using System.Data;

namespace EnrichIped.DataInfrastructure.Repositories;

internal class IpedDevelopmentRepository : IIpedDevelopmentRepository
{
	private readonly string _connectionString;
	private readonly BatchSettings _batchSettings;

	public IpedDevelopmentRepository(IConfiguration configuration)
	{
		var connectionString = configuration.GetConnectionString(IpedDataConstants.ConnectionStringName);

		if (string.IsNullOrWhiteSpace(connectionString))
			throw new Exception(IpedDataConstants.EmptyConnectionStringMessageError);

		var connectionStringBuilder = new MySqlConnectionStringBuilder(connectionString)
		{
			DefaultCommandTimeout = 1800,
			ConnectionTimeout = 120,
			Keepalive = 60,
			CharacterSet = "utf8",
			AllowLoadLocalInfile = true
		};
		_connectionString = connectionStringBuilder.ConnectionString;

		_batchSettings = configuration.GetSection(nameof(BatchSettings)).Get<BatchSettings>() ?? new BatchSettings();
	}

	public async Task<(string, bool)> SaveDevelopmentReportAsync(List<DevelopmentReportDto> developments)
	{
		var minRecordsToSuccess = developments.Count * 0.8;
		try
		{
			try
			{
				return await BulkInsertAsync(developments);
			}
			catch (MySqlException ex) when (ex.Message.Contains("AllowLoadLocalInfile") ||
											ex.ErrorCode == MySqlErrorCode.NotSupportedYet)
			{
				Log.Logger.Warning("BulkCopy não suportado, usando INSERT em lotes: {Error}", ex.Message);
				return await BatchInsertAsync(developments);
			}
		}
		catch (Exception e)
		{
			var errorMessage = $"Erro geral ao tentar inserir dados na tabela 'IpedDevelopmentReport': {e.Message}";
			var tableIndex = e.Message.IndexOf(DevelopmentReportQuery.TableName, StringComparison.Ordinal);
			var textLength = e.Message.Length - tableIndex;
			var errorText = e.Message.Substring(tableIndex, textLength - 1);
			var qtd = errorText.OnlyNumbers();

			if (qtd >= minRecordsToSuccess)
			{
				Log.Logger.Error(errorMessage, e);
				return (errorMessage, true);
			}

			Log.Logger.Error(errorMessage, e);
			return (errorMessage, false);
		}
	}

	private async Task<(string, bool)> BulkInsertAsync(List<DevelopmentReportDto> developments)
	{
		await using var connection = new MySqlConnection(_connectionString);
		await connection.OpenAsync();

		var bulk = new MySqlBulkCopy(connection)
		{
			DestinationTableName = DevelopmentReportQuery.TableName,
			BulkCopyTimeout = 0,
			ConflictOption = MySqlBulkLoaderConflictOption.Replace
		};

		bulk.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(0, nameof(DevelopmentReportDto.CollaboratorId)));
		bulk.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(1, nameof(DevelopmentReportDto.Cpf)));
		bulk.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(2, nameof(DevelopmentReportDto.Name)));
		bulk.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(3, nameof(DevelopmentReportDto.Email)));
		bulk.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(4, nameof(DevelopmentReportDto.Points)));
		bulk.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(5, nameof(DevelopmentReportDto.InProgressCourses)));
		bulk.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(6, nameof(DevelopmentReportDto.CompletedCourses)));
		bulk.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(7, nameof(DevelopmentReportDto.PerformancePercentage)));
		bulk.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(8, nameof(DevelopmentReportDto.CommitmentPercentage)));
		bulk.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(9, nameof(DevelopmentReportDto.EngagementPercentage)));
		bulk.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(10, nameof(DevelopmentReportDto.Score)));
		bulk.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(11, nameof(DevelopmentReportDto.Status)));

		var dataTable = ToFillIpedDevelopmentDataTable(developments);
		var result = await bulk.WriteToServerAsync(dataTable);

		var success = result.RowsInserted > 0;
		var message = success
			? $"BulkCopy: {result.RowsInserted} registros inseridos com sucesso"
			: $"Erro no BulkCopy na tabela '{DevelopmentReportQuery.TableName}'";

		Log.Logger.Information(message);
		return (message, success);
	}

	private async Task<(string, bool)> BatchInsertAsync(List<DevelopmentReportDto> developments)
	{
		var totalRecords = developments.Count;
		var batchSize = _batchSettings.DevelopmentBatchSize;
		var maxParallelBatches = _batchSettings.MaxParallelBatches;
		var minRecords = totalRecords * 0.8;

		Log.Logger.Information(
			$"Iniciando inserção em lotes: {totalRecords} registros, lotes de {batchSize}, máx {maxParallelBatches} paralelos");

		try
		{
			var builder = new MySqlConnectionStringBuilder(_connectionString)
			{
				CharacterSet = "utf8"
			};
			var totalBatches = (int)Math.Ceiling((double)totalRecords / batchSize);

			var semaphore = new SemaphoreSlim(maxParallelBatches, maxParallelBatches);
			var tasks = new List<Task<int>>();

			for (var batchIndex = 0; batchIndex < totalBatches; batchIndex++)
			{
				var batchData = developments
					.Skip(batchIndex * batchSize)
					.Take(batchSize)
					.ToList();

				var task = ProcessBatchWithSemaphore(semaphore, builder, batchData, batchIndex + 1);
				tasks.Add(task);
			}

			var results = await Task.WhenAll(tasks);
			var successfulInserts = results.Sum();

			var successMessage =
				$"Batch INSERT concluído: {successfulInserts} de {totalRecords} registros inseridos em {totalBatches} lotes (máx {maxParallelBatches} paralelos)";
			Log.Logger.Information(successMessage);

			return (successMessage, successfulInserts >= minRecords);
		}
		catch (Exception e)
		{
			var errorMessage = $"Erro no Batch INSERT: {e.Message}";
			Log.Logger.Error(e, errorMessage);
			return (errorMessage, false);
		}
	}

	private async Task<int> ProcessBatchWithSemaphore(
		SemaphoreSlim semaphore,
		MySqlConnectionStringBuilder builder,
		List<DevelopmentReportDto> batchData,
		int batchNumber)
	{
		await semaphore.WaitAsync();
		try
		{
			return await ExecuteBatchInsert(builder, batchData, batchNumber);
		}
		catch (Exception e)
		{
			Log.Logger.Error(e, $"Erro no lote {batchNumber}: {e.Message}");
			return 0;
		}
		finally
		{
			semaphore.Release();
		}
	}

	private static async Task<int> ExecuteBatchInsert(
		MySqlConnectionStringBuilder builder,
		List<DevelopmentReportDto> batchData,
		int batchNumber)
	{
		try
		{
			await using var connection = new MySqlConnection(builder.ConnectionString);
			await connection.OpenAsync();

			var inserted = await connection.ExecuteAsync(
				DevelopmentReportQuery.Insert,
				new
				{
					CollaboratorId = batchData.Select(d => d.CollaboratorId).ToArray(),
					Cpf = batchData.Select(d => d.Cpf).ToArray(),
					Name = batchData.Select(d => d.Name).ToArray(),
					Email = batchData.Select(d => d.Email).ToArray(),
					Points = batchData.Select(d => d.Points).ToArray(),
					InProgressCourses = batchData.Select(d => d.InProgressCourses).ToArray(),
					CompletedCourses = batchData.Select(d => d.CompletedCourses).ToArray(),
					PerformancePercentage = batchData.Select(d => d.PerformancePercentage).ToArray(),
					CommitmentPercentage = batchData.Select(d => d.CommitmentPercentage).ToArray(),
					EngagementPercentage = batchData.Select(d => d.EngagementPercentage).ToArray(),
					Score = batchData.Select(d => d.Score).ToArray(),
					Status = batchData.Select(d => d.Status).ToArray()
				});

			Log.Logger.Debug($"Lote {batchNumber}: {inserted} registros inseridos");
			return inserted;
		}
		catch (Exception e)
		{
			Log.Logger.Error(e, $"Erro ao executar lote {batchNumber}: {e.Message}");
			return 0;
		}
	}

	public async Task<bool> RemoveDuplicatedDevelopmentReportAsync()
	{
		await using var connection = new MySqlConnection(_connectionString);
		await connection.OpenAsync();

		var result = await connection.ExecuteAsync(
			DevelopmentReportQuery.RemoveDuplicatedDevelopmentReport);

		return result > 0;
	}

	private static DataTable ToFillIpedDevelopmentDataTable(List<DevelopmentReportDto> developments)
	{
		var dataTable = GetIpedDevelopmentReportDataTable();

		foreach (var development in developments)
		{
			if (string.IsNullOrWhiteSpace(development.Name))
				continue;

			var cpfRaw = development.Cpf ?? string.Empty;
			var cpf = cpfRaw.PadLeft(11, '0');

			dataTable.Rows.Add(
				development.CollaboratorId.ToString(),
				cpf,
				development.Name.SanitizeString(),
				development.Email.SanitizeString(),
				development.Points.OnlyNumbers(),
				development.InProgressCourses.OnlyNumbers(),
				development.CompletedCourses.OnlyNumbers(),
				development.PerformancePercentage.OnlyNumbers(),
				development.CommitmentPercentage.OnlyNumbers(),
				development.EngagementPercentage.OnlyNumbers(),
				development.Score.OnlyNumbers(),
				development.Status.SanitizeString()
			);
		}

		return dataTable;
	}

	private static DataTable GetIpedDevelopmentReportDataTable()
	{
		var dataTable = new DataTable();
		dataTable.Columns.Add("CollaboratorId", typeof(long));
		dataTable.Columns.Add("Cpf", typeof(string));
		dataTable.Columns.Add("Name", typeof(string));
		dataTable.Columns.Add("Email", typeof(string));
		dataTable.Columns.Add("Points", typeof(int));
		dataTable.Columns.Add("InProgressCourses", typeof(int));
		dataTable.Columns.Add("CompletedCourses", typeof(int));
		dataTable.Columns.Add("PerformancePercentage", typeof(int));
		dataTable.Columns.Add("CommitmentPercentage", typeof(int));
		dataTable.Columns.Add("EngagementPercentage", typeof(int));
		dataTable.Columns.Add("Score", typeof(int));
		dataTable.Columns.Add("Status", typeof(string));

		return dataTable;
	}

	public void Dispose()
	{
		MySqlConnection.ClearAllPools();
	}
}