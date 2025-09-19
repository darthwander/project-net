using EnrichIped.DataInfrastructure.Utilities;

namespace EnrichIped.DataInfrastructure.Queries;

internal static class ConfigurationReportsQuery
{
	private const string FileName = "CREATE TABLE IpedConfigurationReports.sql";
	internal const string TableName = "IpedConfigurationReports";

	internal static readonly string CreateTable =
		SqlScriptLoader.LoadSqlScript(FileName);

	internal const string GetLastConfigId =
		"""
			SELECT MAX(aux.Id) 
			FROM IpedConfigurationReports aux 
			WHERE 1 = 1
			  AND aux.TypeName = @typeName
			  AND LastFileExpiresAt IS NOT NULL	
			  AND LastCompletedSync IS NOT NULL
		""";

	internal const string GetLastFileExpiresAt =
		"""
			SELECT LastFileExpiresAt 
			FROM IpedConfigurationReports
			WHERE 1 = 1
			  AND Id = @id
		""";

	internal const string Insert =
		"""
			INSERT INTO IpedConfigurationReports 
				(TypeName, LastFileName, LastFileExpiresAt)
			VALUES 
				(@typeName, @lastFileName, @lastFileExpiresAt);
			SELECT LAST_INSERT_ID();
		""";

	internal const string SetLastExecution =
		"""
			UPDATE IpedConfigurationReports SET 
				LastExecutionResult = @lastExecutionResult,
				LastExecution = @lastExecution
			WHERE Id = @id
		""";

	internal const string SetLastSync =
		"""
			UPDATE IpedConfigurationReports SET 
				LastCompletedSync = @lastCompletedSync,
				LastCompletedSyncResult = @lastCompletedSyncResult
			WHERE Id = @id
		""";
}