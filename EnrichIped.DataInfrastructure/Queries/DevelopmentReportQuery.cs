using EnrichIped.DataInfrastructure.Utilities;

namespace EnrichIped.DataInfrastructure.Queries;

internal static class DevelopmentReportQuery
{
	private const string FileName = "CREATE TABLE IpedDevelopmentReport.sql";
	internal const string TableName = "IpedDevelopmentReport";

	internal static readonly string CreateTable = SqlScriptLoader.LoadSqlScript(FileName);

	internal const string RemoveDuplicatedDevelopmentReport =
		"""
		    DELETE d1 FROM IpedDevelopmentReport d1
		    INNER JOIN IpedDevelopmentReport d2
		    WHERE d1.Cpf = d2.Cpf
		      AND d1.Id < d2.Id;
		""";

	public const string Insert =
		"""
					INSERT INTO IpedDevelopmentReport
						(`CollaboratorId`
						, `Cpf`
						, `Name`
						, `Email`
						, `Points`
						, `InProgressCourses`
						, `CompletedCourses`
						, `PerformancePercentage`
						, `CommitmentPercentage`
						, `EngagementPercentage`
						, `Score`
						, `Status`)
					VALUES
						( @CollaboratorId
						, @Cpf
						, @Name
						, @Email
						, @Points
						, @InProgressCourses
						, @CompletedCourses
						, @PerformancePercentage
						, @CommitmentPercentage
						, @EngagementPercentage
						, @Score
						, @Status)
		""";
}