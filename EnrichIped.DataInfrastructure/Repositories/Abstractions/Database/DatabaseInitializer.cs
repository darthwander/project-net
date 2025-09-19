using Dapper;

using EnrichIped.DataInfrastructure.Constants;
using EnrichIped.DataInfrastructure.Queries;

using Microsoft.Extensions.Configuration;

using MySqlConnector;

using Serilog;

namespace EnrichIped.DataInfrastructure.Repositories.Abstractions.Database;

public class DatabaseInitializer : IDatabaseInitializer
{
	private readonly string _connectionString;

	private readonly HashSet<TableItem> _initializedTables =
	[
		new(ConfigurationReportsQuery.TableName, ConfigurationReportsQuery.CreateTable),
		new(DevelopmentReportQuery.TableName, DevelopmentReportQuery.CreateTable),
		new(LogReportQuery.TableName, LogReportQuery.CreateTable),
		new(CompleteReportQuery.TableName, CompleteReportQuery.CreateTable)
	];

	public DatabaseInitializer(
		IConfiguration configuration,
		string connectionStringName = IpedDataConstants.ConnectionStringName)
	{
		var connectionString = configuration.GetConnectionString(connectionStringName);

		if (string.IsNullOrEmpty(connectionString))
			throw new ArgumentNullException(
				IpedDataConstants.EmptyConnectionStringMessageError);

		_connectionString = connectionString;
	}

	public async Task InitializeAsync()
	{
		try
		{
			foreach (var item in _initializedTables)
				await EnsureTableExistsAsync(item.TableName, item.CreateTable);
		}
		catch (Exception e)
		{
			Log.Logger.Error(e,
				"Erro ao inicializar o banco de dados. Favor verificar a configuração da Connection string");
		}
	}

	public async Task EnsureTableExistsAsync(string tableName, string createTableQuery)
	{
		try
		{
			await using var connection = new MySqlConnection(_connectionString);
			await connection.OpenAsync();

			var checkTableQuery =
				$"""
				   SELECT COUNT(*) 
				   FROM information_schema.tables 
				   WHERE table_schema = DATABASE() 
				   AND table_name = '{tableName}';
				 """;

			var tableExists = await connection.ExecuteScalarAsync<int>(checkTableQuery);

			if (tableExists == 0)
			{
				await connection.ExecuteAsync(createTableQuery);
				Log.Logger.Information($"Tabela {tableName} criada com sucesso");
			}
		}
		catch (Exception ex)
		{
			Log.Logger.Error(ex, $"Erro ao verificar/criar a tabela {tableName}");
			throw;
		}
	}
}