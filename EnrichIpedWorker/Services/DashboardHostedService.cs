using EnrichIped.BackgroundServices.Constants;

using Hangfire;
using Hangfire.MySql;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

using System.Transactions;

namespace EnrichIped.BackgroundServices.Services;

internal class DashboardHostedService : IHostedService
{
	private WebApplication? _webHost;
	private readonly string? _connectionString;

	public DashboardHostedService(IConfiguration configuration)
	{
		_connectionString = configuration.GetConnectionString(IpedConstants.ConnectionStringName);
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		var builder = WebApplication.CreateBuilder();

		builder.WebHost.UseUrls("http://localhost:5000", "http://0.0.0.0:5000");

		builder.Services.AddHealthChecks();
		builder.Services.AddHangfire(config =>
		{
			config.UseStorage(new MySqlStorage(
				_connectionString,
				new MySqlStorageOptions
				{
					TransactionIsolationLevel = IsolationLevel.ReadCommitted,
					QueuePollInterval = TimeSpan.FromSeconds(15),
					JobExpirationCheckInterval = TimeSpan.FromHours(1),
					CountersAggregateInterval = TimeSpan.FromMinutes(5),
					PrepareSchemaIfNecessary = true,
					TransactionTimeout = TimeSpan.FromMinutes(10),
					TablesPrefix = IpedConstants.HangfireTablesPrefix
				}));
		});

		_webHost = builder.Build();

		var dashboardOptions = new DashboardOptions
		{
			AppPath = null,
			DashboardTitle = "IPED ETL Worker - Hangfire Dashboard",
			StatsPollingInterval = 30000,
			DisplayStorageConnectionString = false
		};

		_webHost.UseHealthChecks("/health");
		_webHost.UseHangfireDashboard("/hangfire", dashboardOptions);
		_webHost.UseRouting();

		return _webHost.StartAsync(cancellationToken);
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		return _webHost?.StopAsync(cancellationToken) ?? Task.CompletedTask;
	}
}