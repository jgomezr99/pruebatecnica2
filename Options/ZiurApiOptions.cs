namespace ZiurInventario.Options;

public sealed class ZiurApiOptions
{
    public const string SectionName = "ZiurApi";
    public string BaseUrl { get; set; } = "https://mainserver.ziursoftware.com/";
    public string Endpoint { get; set; } = "";
    public string BearerToken { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 30;
}
