using System.Net;
using Socks5.Plugin;
using Socks5.Socks;

namespace CaptiveBrowser.Services;

internal class RedirectTraffic : ConnectHandler
{
    private readonly IPEndPoint _proxy;

    public RedirectTraffic(IPEndPoint proxy)
    {
        _proxy = proxy;
    }

    public override bool OnStart()
    {
        return true;
    }

    public override bool Enabled { get; set; } = true;

    public override bool OnConnect(SocksRequest request)
    {
        Console.WriteLine("Redirecting traffic from {0} to {1}.", request.Address, _proxy);
        request.Address = _proxy.ToString();
        request.Type = AddressType.IP;
        request.Port = _proxy.Port;

        return true;
    }
}
