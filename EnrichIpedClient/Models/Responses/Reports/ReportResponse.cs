using Newtonsoft.Json;

namespace EnrichIped.Client.Models.Responses.Reports;

public record ReportResponse
{
	[JsonProperty("STATE")] public int? State { get; set; }

	[JsonProperty("REPORT")] public ReportProperties? Report { get; set; }
}