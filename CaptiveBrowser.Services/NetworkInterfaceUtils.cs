using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace CaptiveBrowser.Services;

public class NetworkInterfaceUtils : INetworkInterfaceUtils
{
    public async Task<Client> CreateClientAsync()
    {
        var interfaceAdapter = await GetBestInterfaceAdapterAsync().ConfigureAwait(false);
        var bindAddress = await GetIpAddressOfInterfaceAsync(interfaceAdapter)
            .ConfigureAwait(false);
        var udp = CreateUdpClient(bindAddress.ip);

        return new Client()
        {
            IpAddress = bindAddress.ip,
            Mac = bindAddress.mac,
            InterfaceName = interfaceAdapter.Name,
            Udp = udp
        };
    }

    private UdpClient CreateUdpClient(IPAddress ip)
    {
        var client = new UdpClient();

        client.Client.Bind(new IPEndPoint(ip, 68));
        client.EnableBroadcast = true;

        return client;
    }

    private Task<(IPAddress ip, PhysicalAddress mac)> GetIpAddressOfInterfaceAsync(
        NetworkInterface adapter
    )
    {
        var stats = adapter.GetIPProperties();

        var ip = stats.UnicastAddresses
            .First((a) => a.Address.AddressFamily == AddressFamily.InterNetwork)
            .Address;

        var mac = adapter.GetPhysicalAddress();

        return Task.FromResult((ip, mac));
    }

    private Task<NetworkInterface> GetBestInterfaceAdapterAsync()
    {
        NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();

        NetworkInterface adapter =
            adapters.FirstOrDefault(
                adapter =>
                    adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
                    && adapter.OperationalStatus == OperationalStatus.Up
            ) ?? throw new Exception("No suitable interface adapter found.");

        var properties = adapter.GetIPProperties();

        if (!properties.IsDnsEnabled && !properties.IsDynamicDnsEnabled)
        {
            throw new Exception("Dns is not enabled for this interface");
        }

        if (!properties.GetIPv4Properties().IsDhcpEnabled)
        {
            throw new Exception("Dhcp is not enabled for this interface");
        }

        return Task.FromResult(adapter);
    }
}
