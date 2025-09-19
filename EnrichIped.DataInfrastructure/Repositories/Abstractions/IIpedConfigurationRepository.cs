namespace EnrichIped.DataInfrastructure.Repositories.Abstractions;

public interface IIpedConfigurationRepository : IDisposable
{
	Task<(long, DateTime?)> GetLastFileExpiresAtAsync(string typeName);

	Task<long> InsertAsync(string typeName, string? lastFileName, DateTime? lastFileExpiresAt);

	Task<bool> SetLastExecutionAsync(long id, string? lastExecutionResult, DateTime? lastExecution);

	Task<bool> SetLastSyncAsync(long id, string? lastCompletedSyncResult, DateTime? lastCompletedSync);
}