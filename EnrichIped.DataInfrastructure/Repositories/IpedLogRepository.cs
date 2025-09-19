using Dapper;

using EnrichIped.DataInfrastructure.Constants;
using EnrichIped.DataInfrastructure.Dtos.LogReport;
using EnrichIped.DataInfrastructure.Extensions;
using EnrichIped.DataInfrastructure.Queries;
using EnrichIped.DataInfrastructure.Repositories.Abstractions;

using Microsoft.Extensions.Configuration;

using MySqlConnector;

using Serilog;

using System.Data;

namespace EnrichIped.DataInfrastructure.Repositories;

internal class IpedLogRepository : IIpedLogRepository
{
	private readonly string? _connectionString;
	private readonly MySqlConnection _connection;

	public IpedLogRepository(IConfiguration configuration)
	{
		_connectionString = configuration.GetConnectionString(IpedDataConstants.ConnectionStringName);

		if (string.IsNullOrWhiteSpace(_connectionString))
			throw new Exception(IpedDataConstants.EmptyConnectionStringMessageError);

		_connection = new MySqlConnection(_connectionString);
	}

	public async Task<DateTime?> GetLastLogReportDateAsync()
	{
		if (_connection.State != ConnectionState.Open)
			await _connection.OpenAsync();

		var lastRecordDate = await _connection.ExecuteScalarAsync<DateTime?>(
			LogReportQuery.GetLastLogReportDate);

		return lastRecordDate;
	}

	public async Task<(string, bool)> SaveLogReportAsync(List<LogReportDto> logs, DateTime? lastRecordDate)
	{
		if (string.IsNullOrWhiteSpace(_connectionString))
			throw new Exception(IpedDataConstants.EmptyConnectionStringMessageError);

		var minRecordsToSuccess = logs.Count * 0.8;
		var builder = new MySqlConnectionStringBuilder(_connectionString)
		{
			AllowLoadLocalInfile = true,
			MaximumPoolSize = 1,
			CharacterSet = "utf8",
			ConnectionTimeout = 120
		};

		try
		{
			await using var batchConnection = new MySqlConnection(builder.ConnectionString);
			await batchConnection.OpenAsync();
			var bulk = new MySqlBulkCopy(batchConnection)
			{
				DestinationTableName = LogReportQuery.TableName,
				BulkCopyTimeout = 0,
				ConflictOption = MySqlBulkLoaderConflictOption.Replace
			};

			var dataTable = ToFillIpedLogDataTable(logs, lastRecordDate);

			if (dataTable.Rows.Count != logs.Count)
				minRecordsToSuccess = dataTable.Rows.Count * 0.8;

			var result = await bulk.WriteToServerAsync(dataTable);

			var success = result.RowsInserted > 0;
			var message = success
				? $"Operação concluída com {result.RowsInserted} registros inseridos com sucesso"
				: $"Houve um erro ao tentar inserir os registros na tabela '{LogReportQuery.TableName}'";

			return (message, success);
		}
		catch (Exception e)
		{
			var errorMessage =
				$"Erro geral ao tentar inserir dados na tabela '{LogReportQuery.TableName}': {e.Message}";
			var tableIndex = e.Message.IndexOf(LogReportQuery.TableName, StringComparison.InvariantCulture);

			if (tableIndex > 1)
			{
				var textLength = e.Message.Length - tableIndex;
				var errorText = e.Message.Substring(tableIndex, textLength - 1);
				var qtd = errorText.OnlyNumbers();

				if (qtd >= minRecordsToSuccess)
				{
					Log.Logger.Error(errorMessage, e);
					return (errorMessage, true);
				}
			}

			Log.Logger.Error(errorMessage, e);
			return (errorMessage, false);
		}
	}

	public async Task<bool> RemoveDuplicatedLogReportAsync()
	{
		if (_connection.State != ConnectionState.Open)
			await _connection.OpenAsync();

		var result = await _connection.ExecuteAsync(LogReportQuery.RemoveDuplicatedLogReport);

		return result > 0;
	}

	private static DataTable GetIpedLogReportDataTable()
	{
		var dataTable = new DataTable();
		dataTable.Columns.Add("CollaboratorId", typeof(long));
		dataTable.Columns.Add("Cpf", typeof(string));
		dataTable.Columns.Add("Name", typeof(string));
		dataTable.Columns.Add("Email", typeof(string));
		dataTable.Columns.Add("Phone", typeof(string));
		dataTable.Columns.Add("CourseId", typeof(int));
		dataTable.Columns.Add("CourseName", typeof(string));
		dataTable.Columns.Add("CourseCategory", typeof(string));
		dataTable.Columns.Add("RecordType", typeof(string));
		dataTable.Columns.Add("RecordDate", typeof(DateTime));
		dataTable.Columns.Add("Reason", typeof(string));
		return dataTable;
	}

	private static DataTable ToFillIpedLogDataTable(List<LogReportDto> logs, DateTime? lastRecordDate)
	{
		var dataTable = GetIpedLogReportDataTable();

		foreach (var log in logs)
		{
			if (log.RecordDate is null)
				continue;

			if (lastRecordDate.HasValue && log.RecordDate <= lastRecordDate.Value)
				continue;

			var cpfRaw = log.Cpf ?? string.Empty;
			var cpf = cpfRaw.PadLeft(11, '0');

			dataTable.Rows.Add(
				log.CollaboratorId.ToString(),
				cpf,
				log.Name.SanitizeString(),
				log.Email.SanitizeString(),
				log.Phone.SanitizeString(),
				log.CourseId.ToString().OnlyNumbers(),
				log.CourseName.SanitizeString(),
				log.CourseCategory.SanitizeString(),
				log.RecordType.SanitizeString(),
				log.RecordDate.ToString().SanitizeDateString(),
				log.Reason.SanitizeString()
			);
		}

		return dataTable;
	}

	public void Dispose()
	{
		MySqlConnection.ClearPoolAsync(_connection).GetAwaiter().GetResult();
		_connection.DisposeAsync().GetAwaiter().GetResult();
	}
}