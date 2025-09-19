using CsvHelper.Configuration.Attributes;

namespace EnrichIped.DataInfrastructure.Dtos.LogReport;

[Delimiter(";")]
[CultureInfo("pt-BR")]
[Encoding("UTF-8")]
public record LogReportDto
{
	[Index(0)] public long? CollaboratorId { get; set; }

	[Index(3)] public string? Cpf { get; set; }

	[Index(2)] public string? Name { get; set; }

	[Index(5)] public string? Email { get; set; }

	[Index(6)] public string? Phone { get; set; }

	[Index(8)] public int? CourseId { get; set; }

	[Index(9)] public string? CourseName { get; set; }

	[Index(7)] public string? CourseCategory { get; set; }

	[Index(10)] public string? RecordType { get; set; }

	[Index(11)] public DateTime? RecordDate { get; set; }

	[Index(12)] public string? Reason { get; set; }
}