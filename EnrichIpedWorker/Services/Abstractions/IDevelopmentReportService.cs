using EnrichIped.Client.Abstractions;
using EnrichIped.Client.Configurations;
using EnrichIped.DataInfrastructure.Repositories.Abstractions;

namespace EnrichIped.BackgroundServices.Services.Abstractions;

public interface IDevelopmentReportService
{
    Task ImportAsync(IpedSettings ipedSettings);

    Task ImportAsync(
		IHttpClientFactory httpFactory,
		IIpedClient ipedClient,
		IIpedDevelopmentRepository repository,
		IIpedConfigurationRepository configurationRepository,
		IpedSettings ipedSettings);
}