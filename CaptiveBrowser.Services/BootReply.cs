namespace CaptiveBrowser.Services;

public record class BootReply
{
    public BootReply()
    {
        DnsServerAddress = String.Empty;
    }

    public string DnsServerAddress { get; init; }
}
