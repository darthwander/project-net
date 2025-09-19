using EnrichIped.BackgroundServices.Constants;
using EnrichIped.BackgroundServices.Services;
using EnrichIped.BackgroundServices.Services.Abstractions;
using EnrichIped.Client.Configurations;

using Hangfire;
using Hangfire.MySql;

using System.Transactions;

using LogLevel = Hangfire.Logging.LogLevel;

namespace EnrichIped.BackgroundServices.Configurations;

internal static class HangfireConfiguration
{
	public static void AddHangfireConfiguration(this IServiceCollection services, string connectionString)
	{
		services.AddTransient<ICompleteReportService, CompleteReportService>();
		services.AddTransient<IDevelopmentReportService, DevelopmentReportService>();
		services.AddTransient<ILogReportService, LogReportService>();

		services.AddHangfire(option =>
		{
			option.UseColouredConsoleLogProvider(LogLevel.Info);
			option.UseSimpleAssemblyNameTypeSerializer();
			option.UseRecommendedSerializerSettings();
			option.UseStorage(new MySqlStorage(
				connectionString,
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

		GlobalConfiguration.Configuration.UseStorage(
			new MySqlStorage(
				connectionString,
				new MySqlStorageOptions
				{
					TransactionIsolationLevel = IsolationLevel.ReadCommitted,
					QueuePollInterval = TimeSpan.FromSeconds(30),
					JobExpirationCheckInterval = TimeSpan.FromHours(1),
					CountersAggregateInterval = TimeSpan.FromMinutes(5),
					PrepareSchemaIfNecessary = true,
					TransactionTimeout = TimeSpan.FromHours(1),
					TablesPrefix = IpedConstants.HangfireTablesPrefix
				}));

		services.AddHangfireServer(options =>
		{
			options.WorkerCount = Environment.ProcessorCount * 5;
			options.Queues = ["default"];
			options.ServerName = Environment.MachineName;
			options.ShutdownTimeout = TimeSpan.FromMinutes(5);
		});
		//services.AddHostedService<DashboardHostedService>();
		services.AddHostedService<StartupJobService>();
	}

	public static void UseHangfire(this IHost host, IConfiguration configuration)
	{
		using var scope = host.Services.CreateScope();
		var ipedSettings = configuration.GetSection(nameof(IpedSettings)).Get<IpedSettings>();

		if (ipedSettings is null)
			throw new ArgumentException(nameof(IpedSettings));

		RecurringJob.AddOrUpdate<ILogReportService>(
			IpedConstants.LogReportJobId,
			service => service.ImportAsync(ipedSettings),
			Cron.Daily(6));

		RecurringJob.AddOrUpdate<IDevelopmentReportService>(
			IpedConstants.DevelopmentReportJobId,
			service => service.ImportAsync(ipedSettings),
			Cron.Daily(6));

		RecurringJob.AddOrUpdate<ICompleteReportService>(
			IpedConstants.CompleteReportJobId,
			service => service.ImportAsync(ipedSettings),
			Cron.Daily(6));
	}
}