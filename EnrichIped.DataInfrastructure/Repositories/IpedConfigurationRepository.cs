using Dapper;

using EnrichIped.DataInfrastructure.Constants;
using EnrichIped.DataInfrastructure.Queries;
using EnrichIped.DataInfrastructure.Repositories.Abstractions;

using Microsoft.Extensions.Configuration;

using MySqlConnector;

namespace EnrichIped.DataInfrastructure.Repositories;

internal class IpedConfigurationRepository : IIpedConfigurationRepository
{
	private readonly string _connectionString;

	public IpedConfigurationRepository(IConfiguration configuration)
	{
		var connectionString = configuration.GetConnectionString(IpedDataConstants.ConnectionStringName);

		if (string.IsNullOrWhiteSpace(connectionString))
			throw new Exception(IpedDataConstants.EmptyConnectionStringMessageError);

		var connectionStringBuilder = new MySqlConnectionStringBuilder(connectionString)
		{
			DefaultCommandTimeout = 1800,
			ConnectionTimeout = 120,
			Keepalive = 60
		};
		_connectionString = connectionStringBuilder.ConnectionString;
	}

	public async Task<(long, DateTime?)> GetLastFileExpiresAtAsync(string typeName)
	{
		await using var connection = new MySqlConnection(_connectionString);
		await connection.OpenAsync();

		var lastId = await connection.QueryFirstOrDefaultAsync<long?>(
			ConfigurationReportsQuery.GetLastConfigId,
			new
			{
				typeName
			});

		if (lastId is null or 0)
			return (0, null);

		var lastRecordDate = await connection.ExecuteScalarAsync<DateTime?>(
			ConfigurationReportsQuery.GetLastFileExpiresAt,
			new
			{
				id = lastId
			});

		return (lastId.Value, lastRecordDate);
	}

	public async Task<long> InsertAsync(string typeName, string? lastFileName, DateTime? lastFileExpiresAt)
	{
		await using var connection = new MySqlConnection(_connectionString);
		await connection.OpenAsync();

		var result = await connection.QuerySingleAsync<long>(
			ConfigurationReportsQuery.Insert,
			new
			{
				typeName,
				lastFileName,
				lastFileExpiresAt
			});

		return result;
	}

	public async Task<bool> SetLastExecutionAsync(long id, string? lastExecutionResult, DateTime? lastExecution)
	{
		await using var connection = new MySqlConnection(_connectionString);
		await connection.OpenAsync();

		var result = await connection.ExecuteAsync(
			ConfigurationReportsQuery.SetLastExecution,
			new
			{
				id,
				lastExecutionResult,
				lastExecution
			});

		return result > 0;
	}

	public async Task<bool> SetLastSyncAsync(long id, string? lastCompletedSyncResult, DateTime? lastCompletedSync)
	{
		await using var connection = new MySqlConnection(_connectionString);
		await connection.OpenAsync();

		var result = await connection.ExecuteAsync(
			ConfigurationReportsQuery.SetLastSync,
			new
			{
				id,
				lastCompletedSyncResult,
				lastCompletedSync
			});

		return result > 0;
	}

	public void Dispose()
	{
		MySqlConnection.ClearAllPools();
	}
}