namespace EnrichIped.Client.Configurations;

public class IpedSettings
{
    public string? Uri { get; set; }
    public string? Token { get; set; }
    public string? TokenUdt { get; set; }
    
    public IEnumerable<string> GetAllTokens()
    {
        if (!string.IsNullOrWhiteSpace(Token))
            yield return Token;
            
        if (!string.IsNullOrWhiteSpace(TokenUdt))
            yield return TokenUdt;
    }
}