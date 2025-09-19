using EnrichIped.DataInfrastructure.Dtos.LogReport;

namespace EnrichIped.DataInfrastructure.Repositories.Abstractions;

public interface IIpedLogRepository : IDisposable
{
	Task<DateTime?> GetLastLogReportDateAsync();
	Task<(string, bool)> SaveLogReportAsync(List<LogReportDto> logs, DateTime? lastRecordDate);
	Task<bool> RemoveDuplicatedLogReportAsync();
}