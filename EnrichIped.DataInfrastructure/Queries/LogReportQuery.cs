using EnrichIped.DataInfrastructure.Utilities;

namespace EnrichIped.DataInfrastructure.Queries;

internal static class LogReportQuery
{
	private const string FileName = "CREATE TABLE IpedLogReport.sql";
	internal const string TableName = "IpedLogReport";

	internal static readonly string CreateTable =
		SqlScriptLoader.LoadSqlScript(FileName);

	internal const string GetLastLogReportDate = "SELECT MAX(RecordDate) FROM IpedLogReport";

	internal const string RemoveDuplicatedLogReport =
		"""
		    DELETE FROM IpedLogReport
		    WHERE Id NOT IN (
		        SELECT MAX(Id)
		        FROM IpedLogReport
		        GROUP BY Cpf, CourseId, RecordType, RecordDate
		        )
		""";
}