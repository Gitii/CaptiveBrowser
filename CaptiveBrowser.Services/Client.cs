using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace CaptiveBrowser.Services;

public record class Client : IDisposable
{
    public PhysicalAddress Mac { get; init; } = PhysicalAddress.None;
    public IPAddress IpAddress { get; init; } = IPAddress.None;
    public string InterfaceName { get; init; } = String.Empty;
    public UdpClient Udp { get; init; } = null!;

    public void Dispose()
    {
        Udp.Dispose();
    }
}
