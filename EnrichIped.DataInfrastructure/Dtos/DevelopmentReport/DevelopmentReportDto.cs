using CsvHelper.Configuration.Attributes;

namespace EnrichIped.DataInfrastructure.Dtos.DevelopmentReport;

[Delimiter(";")]
[CultureInfo("pt-BR")]
[Encoding("UTF-8")]
public record DevelopmentReportDto
{
    [Index(0)] public long? CollaboratorId { get; set; }

	[Index(3)] public string? Cpf { get; set; }

	[Index(2)] public string? Name { get; set; }

    [Index(4)] public string? Position { get; set; }

    [Index(5)] public string? Email { get; set; }

    [Index(6)] public string? Points { get; set; }

	[Index(7)] public string? InProgressCourses { get; set; }

	[Index(8)] public string? CompletedCourses { get; set; }

	[Index(9)] public string? PerformancePercentage { get; set; }

	[Index(10)] public string? CommitmentPercentage { get; set; }

	[Index(11)] public string? EngagementPercentage { get; set; }

	[Index(12)] public string? Score { get; set; }

	[Index(13)] public string? Status { get; set; }
}