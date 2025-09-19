using EnrichIped.BackgroundServices.Configurations;
using EnrichIped.BackgroundServices.Constants;
using EnrichIped.Client;
using EnrichIped.DataInfrastructure;
using EnrichIped.DataInfrastructure.Repositories.Abstractions.Database;

using System.Reflection;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
	.AddJsonFile(IpedConstants.AppSettingsJson, true, true)
	.AddEnvironmentVariables();

if (builder.Environment.IsDevelopment())
{
	builder.Configuration.AddJsonFile(IpedConstants.AppSettingsDevelopmentJson, true);
	builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly());
}

builder.Services.AddLogConfiguration(builder.Configuration);

var connectionString = builder.Configuration.GetConnectionString(IpedConstants.ConnectionStringName);

if (string.IsNullOrWhiteSpace(connectionString))
	throw new Exception(IpedConstants.EmptyConnectionStringErrorMessage);

var connectionStringBuilder = new MySqlConnector.MySqlConnectionStringBuilder(connectionString)
{
	DefaultCommandTimeout = 1800,
	ConnectionTimeout = 120,
	Keepalive = 60,
	AllowLoadLocalInfile = true
};
connectionString = connectionStringBuilder.ConnectionString;

builder.Services.AddHttpClient();
builder.Services.AddIpedClient(builder.Configuration);
builder.Services.AddDataInfrastructure();
builder.Services.AddHangfireConfiguration(connectionString);

var host = builder.Build();

host.UseHangfire(builder.Configuration);

var verifyTables = host.Services.GetRequiredService<IDatabaseInitializer>();
await verifyTables.InitializeAsync();

host.Run();