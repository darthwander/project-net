using CsvHelper.Configuration;

using EnrichIped.DataInfrastructure.Dtos.CompleteReport;

namespace EnrichIped.BackgroundServices.Maps;

public sealed class CompleteReportDtoMap : ClassMap<CompleteReportDto>
{
	public CompleteReportDtoMap()
	{
		Map(m => m.CollaboratorId).Index(0).TypeConverterOption.NullValues("0");
		Map(m => m.Name).Index(2).TypeConverterOption.NullValues("");
		Map(m => m.Cpf).Index(3).TypeConverterOption.NullValues("");
		Map(m => m.Position).Index(4).TypeConverterOption.NullValues("");
		Map(m => m.Email).Index(5).TypeConverterOption.NullValues("");
		Map(m => m.Group).Index(6).TypeConverterOption.NullValues("");
		Map(m => m.SubGroup1).Index(7).TypeConverterOption.NullValues("");
		Map(m => m.SubGroup2).Index(8).TypeConverterOption.NullValues("");
		Map(m => m.EducationTrack).Index(9).TypeConverterOption.NullValues("");
		Map(m => m.CourseId).Index(10).TypeConverterOption.NullValues("");
		Map(m => m.CourseName).Index(11).TypeConverterOption.NullValues("");
		Map(m => m.CourseCategory).Index(12).TypeConverterOption.NullValues("");
		Map(m => m.CourseProgress).Index(13).TypeConverterOption.NullValues("");
		Map(m => m.ReleaseDate).Index(14).TypeConverterOption.NullValues("");
		Map(m => m.StartDate).Index(15).TypeConverterOption.NullValues("");
		Map(m => m.EndDate).Index(17).TypeConverterOption.NullValues("");
		Map(m => m.LastAccess).Index(16).TypeConverterOption.NullValues("");
		Map(m => m.Duration).Index(18).TypeConverterOption.NullValues("");
		Map(m => m.PerformanceRate).Index(19).TypeConverterOption.NullValues("");
		Map(m => m.Mandatory).Index(20).TypeConverterOption.NullValues("");
		Map(m => m.Status).Index(21).TypeConverterOption.NullValues("");
	}
}