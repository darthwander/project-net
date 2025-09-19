using CsvHelper.Configuration.Attributes;

namespace EnrichIped.DataInfrastructure.Dtos.CompleteReport;

[Delimiter(";")]
[CultureInfo("pt-BR")]
[Encoding("UTF-8")]
public record CompleteReportDto
{
	[Index(0)] public long? CollaboratorId { get; set; }

	[Index(3)] public string? Cpf { get; set; }

	[Index(2)] public string? Name { get; set; }

	[Index(4)] public string? Position { get; set; }

	[Index(5)] public string? Email { get; set; }

	[Index(6)] public string? Group { get; set; }

	[Index(7)] public string? SubGroup1 { get; set; }

	[Index(8)] public string? SubGroup2 { get; set; }

	[Index(9)] public string? EducationTrack { get; set; }

	[Index(10)] public long? CourseId { get; set; }

	[Index(11)] public string? CourseName { get; set; }

	[Index(12)] public string? CourseCategory { get; set; }

	[Index(13)] public string? CourseProgress { get; set; }

	[Index(14)] public string? ReleaseDate { get; set; }

	[Index(15)] public string? StartDate { get; set; }

	[Index(17)] public string? EndDate { get; set; }

	[Index(16)] public string? LastAccess { get; set; }

	[Index(18)] public string? Duration { get; set; }

	[Index(19)] public string? PerformanceRate { get; set; }

	[Index(20)] public string? Mandatory { get; set; }

	[Index(21)] public string? Status { get; set; }
}