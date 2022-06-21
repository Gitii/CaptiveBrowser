using System;
using Socks5.Socks;

namespace Socks5.TCP;

public class SocksClientEventArgs : EventArgs
{
    public SocksClient Client { get; private set; }

    public SocksClientEventArgs(SocksClient client)
    {
        Client = client;
    }
}
