namespace EnrichIped.Client.Constants;

public static partial class IpedClientConstants
{
    #region Default Headers

    public const string ContentTypeHeader = "Content-Type: application/json";
    public const string AcceptHeader = "accept: application/json";

    #endregion

    public const string GetReports = "/api/corporate/get-report?token={token}&type={type}";
}
