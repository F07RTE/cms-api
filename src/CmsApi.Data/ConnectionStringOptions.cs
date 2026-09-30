namespace CmsApi.Data;

public sealed class ConnectionStringOptions
{
    public const string SectionName = "ConnectionStrings";

    public string Reader { get; set; } = string.Empty;

    public string Writer { get; set; } = string.Empty;
}
