using EnrichIped.DataInfrastructure.Dtos.CompleteReport;

namespace EnrichIped.DataInfrastructure.Repositories.Abstractions;

public interface IIpedCompleteRepository : IDisposable
{
	Task<(string, bool)> SaveCompleteReportAsync(List<CompleteReportDto> completeData);
	Task<bool> RemoveDuplicatedCompleteReportAsync();
}