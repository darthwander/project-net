using EnrichIped.Client.Abstractions;
using EnrichIped.Client.Configurations;
using EnrichIped.DataInfrastructure.Repositories.Abstractions;

namespace EnrichIped.BackgroundServices.Services.Abstractions;

public interface ICompleteReportService
{
	Task ImportAsync(
		IHttpClientFactory httpFactory,
		IIpedClient ipedClient,
		IIpedCompleteRepository repository,
		IIpedConfigurationRepository configurationRepository,
		IpedSettings ipedSettings);

	Task ImportAsync(IpedSettings ipedSettings);
}