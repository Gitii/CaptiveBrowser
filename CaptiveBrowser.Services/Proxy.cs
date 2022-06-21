using System.Net;
using Socks5.Plugin;
using Socks5.SocksServer;

namespace CaptiveBrowser.Services;

class Proxy : IProxy
{
    private Socks5Server? _server;
    private IPEndPoint? _endPoint;

    public async Task<IPEndPoint> StartAsync(IPEndPoint proxy)
    {
        if (_server == null || _endPoint == null)
        {
            var endpoint = new IPEndPoint(IPAddress.Loopback, 0);
            var server = new Socks5Server(endpoint.Address, endpoint.Port);
            PluginLoader.Plugins.Add(new RedirectTraffic(proxy));

            _server = server;
            _endPoint = endpoint;
            server.Start();
        }

        return _endPoint;
    }

    public void Stop()
    {
        _server?.Stop();
        _server = null;
    }

    public void Dispose()
    {
        Stop();
    }
}
