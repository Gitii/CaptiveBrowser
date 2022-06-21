using System.Net;

namespace Socks5.Socks;

public class User
{
    public string Username { get; private set; }
    public string Password { get; private set; }
    public IPEndPoint IP { get; private set; }

    public User(string un, string pw, IPEndPoint ip)
    {
        Username = un;
        Password = pw;
        IP = ip;
    }
}
