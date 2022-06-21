namespace CaptiveBrowser.Services;

public interface INetworkInterfaceUtils
{
    Task<Client> CreateClientAsync();
}
