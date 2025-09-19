using Newtonsoft.Json;

namespace EnrichIped.Client.Models.Responses.Reports;

public record ReportProperties
{
	[JsonProperty("report_status")] public string? Status { get; set; }

	[JsonProperty("report_expires_at")] public string? ExpiresAt { get; set; }

	[JsonProperty("report_file")] public string? File { get; set; }

	[JsonProperty("report_type")] public string? Type { get; set; }
}