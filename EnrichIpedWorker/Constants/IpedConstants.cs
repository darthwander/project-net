namespace EnrichIped.BackgroundServices.Constants;

public static class IpedConstants
{
	public const string AppSettingsJson = "appsettings.json";
	public const string AppSettingsDevelopmentJson = "appsettings.Development.json";

	public const string ConnectionStringName = "Finance";

	public const string EmptyConnectionStringErrorMessage =
		"CONNECTION STRING is not set => Check appsettings.json file please";

	public const string HangfireTablesPrefix = "Hangfire";
	public const string EverySixHourlyExpression = "0 */6 * * *";
	public const string EveryTowHourlyExpression = "0 */2 * * *";

	#region IPED

	public const string CompletedReportStatus = "completed";

	public const string LogReportJobId = "enrich-iped-log-report";
	public const string DevelopmentReportJobId = "enrich-iped-development-report";
	public const string CompleteReportJobId = "enrich-iped-complete-report";

	public const string LogServiceType = "log";
	public const string DevelopmentServiceType = "development";
	public const string CompleteServiceType = "complete";

	#endregion
}