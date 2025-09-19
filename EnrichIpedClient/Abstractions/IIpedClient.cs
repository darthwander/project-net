using EnrichIped.Client.Constants;
using EnrichIped.Client.Models.Responses.Reports;
using Refit;

namespace EnrichIped.Client.Abstractions;

[Headers(IpedClientConstants.ContentTypeHeader, IpedClientConstants.AcceptHeader)]
public interface IIpedClient
{
    [Post(IpedClientConstants.GetReports)]
    public Task<ApiResponse<ReportResponse>> GetReportAsync(string token, string type);
}