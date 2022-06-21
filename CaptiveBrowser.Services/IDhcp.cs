namespace CaptiveBrowser.Services;

public interface IDhcp
{
    Task<string> DiscoverDnsServerAsync(Client client);
}
