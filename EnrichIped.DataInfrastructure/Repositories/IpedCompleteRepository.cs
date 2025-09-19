using Dapper;

using EnrichIped.DataInfrastructure.Constants;
using EnrichIped.DataInfrastructure.Dtos.CompleteReport;
using EnrichIped.DataInfrastructure.Extensions;
using EnrichIped.DataInfrastructure.Queries;
using EnrichIped.DataInfrastructure.Repositories.Abstractions;

using Microsoft.Extensions.Configuration;

using MySqlConnector;

using Serilog;

using System.Data;

namespace EnrichIped.DataInfrastructure.Repositories;

internal class IpedCompleteRepository : IIpedCompleteRepository
{
	private readonly MySqlConnection _connection;

	public IpedCompleteRepository(IConfiguration configuration)
	{
		var connectionString = configuration.GetConnectionString(IpedDataConstants.ConnectionStringName);

		if (string.IsNullOrWhiteSpace(connectionString))
			throw new Exception(IpedDataConstants.EmptyConnectionStringMessageError);

		_connection = new MySqlConnection(connectionString);
	}

	public async Task<(string, bool)> SaveCompleteReportAsync(List<CompleteReportDto> completeData)
	{
		const int batchSize = 25000;
		var totalRecords = completeData.Count;
		var totalBatches = (int)Math.Ceiling((double)totalRecords / batchSize);
		var minRecords = totalRecords * 0.8;
		var successfulInserts = 0;
		var failedBatches = 0;

		var builder = new MySqlConnectionStringBuilder(_connection.ConnectionString)
		{
			AllowLoadLocalInfile = true,
			MaximumPoolSize = 1,
			CharacterSet = "utf8",
			ConnectionTimeout = 120
		};

		try
		{
			for (var batchIndex = 0; batchIndex < totalBatches; batchIndex++)
			{
				var startIndex = batchIndex * batchSize;
				var count = Math.Min(batchSize, totalRecords - startIndex);
				var batchData = completeData.GetRange(startIndex, count);

				Log.Logger.Information(
					$"Processando lote {batchIndex + 1} de {totalBatches} (registros {startIndex + 1} até {startIndex + count} de {totalRecords})");

				try
				{
					successfulInserts += await BulkInsertAsync(builder, batchData, count, batchIndex);
				}
				catch (MySqlException me)
				{
					failedBatches++;
					Log.Logger.Error($"Erro MySQL no lote {batchIndex + 1}: {me.Message}", me);
				}
				catch (Exception e)
				{
					failedBatches++;
					Log.Logger.Error($"Erro ao inserir lote {batchIndex + 1}: {e.Message}", e);
				}

				if (batchIndex < totalBatches - 1) await Task.Delay(500);
			}

			var successMessage =
				$"Operação concluída: {successfulInserts} de {totalRecords} registros inseridos com sucesso em {totalBatches - failedBatches} de {totalBatches} lotes";
			Log.Logger.Information(successMessage);

			return (successMessage, successfulInserts > minRecords);
		}
		catch (Exception e)
		{
			var errorMessage = $"Erro geral ao tentar inserir dados na tabela 'IpedCompleteReport': {e.Message}";
			Log.Logger.Error(errorMessage, e);
			return (errorMessage, false);
		}
	}

	private static async Task<int> BulkInsertAsync(
		MySqlConnectionStringBuilder builder,
		List<CompleteReportDto> batchData,
		int count,
		int batchIndex)
	{
		var successfulInserts = 0;
		await using var batchConnection = new MySqlConnection(builder.ConnectionString);
		await batchConnection.OpenAsync();

		var bulk = new MySqlBulkCopy(batchConnection)
		{
			DestinationTableName = CompleteReportQuery.TableName,
			BulkCopyTimeout = 0,
			ConflictOption = MySqlBulkLoaderConflictOption.Replace
		};

		var dataTable = ToFillIpedCompleteDataTable(batchData);
		var result = await bulk.WriteToServerAsync(dataTable);

		if (result.RowsInserted == count)
		{
			successfulInserts += result.RowsInserted;
			Log.Logger.Information(
				$"Lote {batchIndex + 1} inserido com sucesso: {result.RowsInserted} registros");
		}
		else
		{
			successfulInserts += result.RowsInserted;
			Log.Logger.Warning(
				$"Inserção parcial no lote {batchIndex + 1}: {result.RowsInserted} de {count} registros inseridos");
		}

		return successfulInserts;
	}

    public async Task<bool> RemoveDuplicatedCompleteReportAsync()
    {
        const int batchSize = 1000;
        var totalDeleted = 0;
        int deletedInBatch;

        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync();

        do
        {
            deletedInBatch = await _connection.ExecuteAsync(
                CompleteReportQuery.RemoveDuplicatedCompleteReportBatch,
                new { BatchSize = batchSize });

            totalDeleted += deletedInBatch;

            if (deletedInBatch > 0)
                await Task.Delay(500);

        } while (deletedInBatch > 0);

        return totalDeleted > 0;
    }

    private static DataTable GetIpedCompleteReportDataTable()
	{
		var dataTable = new DataTable();
		dataTable.Columns.Add("Id", typeof(long)); //0
		dataTable.Columns.Add("AccountId", typeof(string)); //1
		dataTable.Columns.Add("CollaboratorId", typeof(long)); //2
		dataTable.Columns.Add("Name", typeof(string)); //3
		dataTable.Columns.Add("Cpf", typeof(string)); //4
		dataTable.Columns.Add("Position", typeof(string)); //5
		dataTable.Columns.Add("Email", typeof(string)); //6
		dataTable.Columns.Add("Group", typeof(string)); //7
		dataTable.Columns.Add("SubGroup1", typeof(string)); //8
		dataTable.Columns.Add("SubGroup2", typeof(string)); //9
		dataTable.Columns.Add("EducationTrack", typeof(string)); //10
		dataTable.Columns.Add("CourseId", typeof(long)); //11
		dataTable.Columns.Add("CourseName", typeof(string)); //12
		dataTable.Columns.Add("CourseCategory", typeof(string)); //13
		dataTable.Columns.Add("CourseProgress", typeof(string)); //14
		dataTable.Columns.Add("ReleaseDate", typeof(DateTime)); //15
		dataTable.Columns.Add("StartDate", typeof(DateTime)); //16
		dataTable.Columns.Add("EndDate", typeof(DateTime)); //17
		dataTable.Columns.Add("LastAccess", typeof(DateTime)); //18
		dataTable.Columns.Add("Duration", typeof(string)); //19
		dataTable.Columns.Add("PerformanceRate", typeof(string)); //20
		dataTable.Columns.Add("Mandatory", typeof(string)); //21
		dataTable.Columns.Add("Status", typeof(string)); //22

		return dataTable;
	}

	private static DataTable ToFillIpedCompleteDataTable(List<CompleteReportDto> completeData)
	{
		const string courseNotFound = "Nenhum curso iniciado";
		var dataTable = GetIpedCompleteReportDataTable();

		foreach (var item in completeData)
		{
			if (string.IsNullOrWhiteSpace(item.Cpf)
				|| (!string.IsNullOrWhiteSpace(item.CourseName)
					&& item.CourseName.Contains(courseNotFound))
				|| item.CourseId is null or 0)
				continue;

			try
			{
				var row = dataTable.NewRow();
				var cpfRaw = item.Cpf ?? string.Empty;
				var cpf = cpfRaw.PadLeft(11, '0');

				row[2] = item.CollaboratorId;
				row[3] = item.Name.SanitizeString();
				row[4] = cpf;
				row[5] = item.Position.SanitizeString();
				row[6] = item.Email.SanitizeString();
				row[7] = item.Group.SanitizeString();
				row[8] = item.SubGroup1.SanitizeString();
				row[9] = item.SubGroup2.SanitizeString();
				row[10] = item.EducationTrack.SanitizeString();
				row[11] = item.CourseId;
				row[12] = item.CourseName.SanitizeString();
				row[13] = item.CourseCategory.SanitizeString();
				row[14] = item.CourseProgress.SanitizeString();
				row[15] = item.ReleaseDate.SanitizeDateString();
				row[16] = item.StartDate.SanitizeDateString();
				row[17] = item.EndDate.SanitizeDateString();
				row[18] = item.LastAccess.SanitizeDateString();
				row[19] = item.Duration.SanitizeString();
				row[20] = item.PerformanceRate.SanitizeString();
				row[21] = item.Mandatory.SanitizeString();
				row[22] = item.Status.SanitizeString();

				dataTable.Rows.Add(row);
			}
			catch (Exception e)
			{
				Log.Logger.Error(
					$"Erro ao adicionar registro CPF:'{item.Cpf}' e Curso:'{item.CourseName}' na DataTable: {e.Message}",
					e);
			}
		}

		return dataTable;
	}

	public void Dispose()
	{
		MySqlConnection.ClearPoolAsync(_connection).GetAwaiter().GetResult();
		_connection.DisposeAsync().GetAwaiter().GetResult();
	}
}