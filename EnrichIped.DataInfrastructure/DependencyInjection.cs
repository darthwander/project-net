using EnrichIped.DataInfrastructure.Repositories;
using EnrichIped.DataInfrastructure.Repositories.Abstractions;
using EnrichIped.DataInfrastructure.Repositories.Abstractions.Database;

using Microsoft.Extensions.DependencyInjection;

namespace EnrichIped.DataInfrastructure;

public static class DependencyInjection
{
	public static void AddDataInfrastructure(
		this IServiceCollection services)
	{
		services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
		services.AddTransient<IIpedConfigurationRepository, IpedConfigurationRepository>();
		services.AddTransient<IIpedCompleteRepository, IpedCompleteRepository>();
		services.AddTransient<IIpedLogRepository, IpedLogRepository>();
		services.AddTransient<IIpedDevelopmentRepository, IpedDevelopmentRepository>();
	}
}