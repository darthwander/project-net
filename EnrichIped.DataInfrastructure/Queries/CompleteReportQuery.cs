using EnrichIped.DataInfrastructure.Utilities;

namespace EnrichIped.DataInfrastructure.Queries;

internal static class CompleteReportQuery
{
	private const string FileName = "CREATE TABLE IpedCompleteReport.sql";
	internal const string TableName = "IpedCompleteReport";

	internal static readonly string CreateTable =
		SqlScriptLoader.LoadSqlScript(FileName);

	internal const string GetLastCompleteReportDate = "SELECT MAX(CreatedAt) FROM IpedCompleteReport";

	internal const string RemoveDuplicatedCompleteReport =
		"""
		    DELETE FROM IpedCompleteReport
		    WHERE Id NOT IN (
		        SELECT MAX(Id)
		        FROM (
		            SELECT icr.CollaboratorId, icr.CourseId
		            FROM IpedCompleteReport icr
		            GROUP BY icr.CollaboratorId, icr.CourseId
		        ) AS temp
		    );
		""";

    internal const string RemoveDuplicatedCompleteReportBatch =
        """
        DELETE TOP (@BatchSize) FROM IpedCompleteReport
        WHERE Id NOT IN (
            SELECT MAX(Id)
            FROM IpedCompleteReport
            GROUP BY CollaboratorId, CourseId
        );
        """;
}