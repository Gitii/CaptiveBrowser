using System;
using System.Net;
using System.Text;

namespace Socks5.Socks;

public class SocksRequest
{
    public AddressType Type { get; set; }
    public StreamTypes StreamType { get; private set; }
    public string? Address { get; set; }
    public int Port { get; set; }
    public SocksError Error { get; set; }

    public SocksRequest(StreamTypes type, AddressType addrtype, string address, int port)
    {
        Type = addrtype;
        StreamType = type;
        Address = address;
        Port = port;
        Error = SocksError.Granted;
        IPAddress? p = this.IP; //get Error on the stack.
    }

    public IPAddress? IP
    {
        get
        {
            if (Type == AddressType.IP)
            {
                try
                {
                    return IPAddress.Parse(Address);
                }
                catch
                {
                    Error = SocksError.NotSupported;
                    return null;
                }
            }
            else if (Type == AddressType.Domain)
            {
                try
                {
                    foreach (IPAddress p in Dns.GetHostAddresses(Address))
                    {
                        if (p.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            return p;
                        }
                    }

                    return null;
                }
                catch
                {
                    Error = SocksError.HostUnreachable;
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
    }

    public byte[] GetData(bool NetworkToHostOrder)
    {
        byte[] data;
        var port = 0;
        if (NetworkToHostOrder)
        {
            port = IPAddress.NetworkToHostOrder(Port);
        }
        else
        {
            port = IPAddress.HostToNetworkOrder((short)Port);
        }

        if (Type == AddressType.IP)
        {
            data = new byte[10];
            string[] content = IP!.ToString().Split('.');
            for (int i = 4; i < content.Length + 4; i++)
            {
                data[i] = Convert.ToByte(Convert.ToInt32(content[i - 4]));
            }

            Buffer.BlockCopy(BitConverter.GetBytes(port), 0, data, 8, 2);
        }
        else if (Type == AddressType.Domain)
        {
            data = new byte[Address!.Length + 7];
            data[4] = Convert.ToByte(Address.Length);
            Buffer.BlockCopy(Encoding.ASCII.GetBytes(Address), 0, data, 5, Address.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(port), 0, data, data.Length - 2, 2);
        }
        else
        {
            return null;
        }

        data[0] = 0x05;
        data[1] = (byte)Error;
        data[2] = 0x00;
        data[3] = (byte)Type;
        return data;
    }
}
