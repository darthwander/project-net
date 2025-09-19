using EnrichIped.Client.Abstractions;
using EnrichIped.Client.Configurations;
using EnrichIped.Client.Constants;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Refit;

namespace EnrichIped.Client;

public static class DependencyInjection
{
	public static void AddIpedClient(this IServiceCollection services, IConfiguration configuration)
	{
		var config = configuration.GetSection(IpedClientConstants.IpedConfigKey).Get<IpedSettings>();

		if (config is null
			|| string.IsNullOrEmpty(config.Uri)
			|| string.IsNullOrWhiteSpace(config.Token))
			throw new ArgumentNullException(IpedClientConstants.UriConfigMissing);

		services.AddSingleton(config);

		services.AddRefitClient<IIpedClient>(_ => new RefitSettings
		{
			ContentSerializer = new NewtonsoftJsonContentSerializer()
		})
			.ConfigureHttpClient(client =>
			{
				client.BaseAddress = new Uri(config.Uri);
				client.DefaultRequestHeaders.Add(IpedClientConstants.Authorization, config.Token);
			});
	}
}