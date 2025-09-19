using EnrichIped.DataInfrastructure.Dtos.DevelopmentReport;

namespace EnrichIped.DataInfrastructure.Repositories.Abstractions;

public interface IIpedDevelopmentRepository : IDisposable
{
	Task<(string, bool)> SaveDevelopmentReportAsync(List<DevelopmentReportDto> developments);
	Task<bool> RemoveDuplicatedDevelopmentReportAsync();
}