/*
    Socks5 - A full-fledged high-performance socks5 proxy server written in C#. Plugin support included.
    Copyright (C) 2016 ThrDev

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Socks5.Encryption;
using Socks5.TCP;

namespace Socks5.Socks;

class Socks5
{
    public static List<AuthTypes> RequestAuth(SocksClient client)
    {
        byte[] buff;
        int recv = Receive(client.Client, out buff);

        if (buff == null || (HeaderTypes)buff[0] != HeaderTypes.Socks5)
        {
            return new List<AuthTypes>();
        }

        int methods = Convert.ToInt32(buff[1]);
        List<AuthTypes> types = new List<AuthTypes>();
        for (int i = 2; i < methods + 2; i++)
        {
            switch ((AuthTypes)buff[i])
            {
                case AuthTypes.Login:
                    types.Add(AuthTypes.Login);
                    break;
                case AuthTypes.None:
                    types.Add(AuthTypes.None);
                    break;
                case AuthTypes.SocksBoth:
                    types.Add(AuthTypes.SocksBoth);
                    break;
                case AuthTypes.SocksEncrypt:
                    types.Add(AuthTypes.SocksEncrypt);
                    break;
                case AuthTypes.SocksCompress:
                    types.Add(AuthTypes.SocksCompress);
                    break;
            }
        }

        return types;
    }

    public static SocksEncryption RequestSpecialMode(List<AuthTypes> auth, Client client)
    {
        //select mode, do key exchange if encryption, or start compression.
        if (auth.Contains(AuthTypes.SocksBoth))
        {
            //tell client that we chose socksboth.
            client.Send(new byte[] { (byte)HeaderTypes.Socks5, (byte)AuthTypes.SocksBoth });
            //wait for public key.
            SocksEncryption ph = new SocksEncryption();
            ph.GenerateKeys();
            //wait for public key.
            byte[] buffer = new byte[4096];
            int keysize = client.Receive(buffer, 0, buffer.Length);
            //store key in our encryption class.
            ph.SetKey(buffer, 0, keysize);
            //send key.
            client.Send(ph.GetPublicKey());
            //now we give them our key.
            client.Send(ph.ShareEncryptionKey());
            //send more.
            int enckeysize = client.Receive(buffer, 0, buffer.Length);
            //decrypt with our public key.
            byte[] newkey = new byte[enckeysize];
            Buffer.BlockCopy(buffer, 0, newkey, 0, enckeysize);
            ph.SetEncKey(ph.key.Decrypt(newkey, false));

            ph.SetType(AuthTypes.SocksBoth);
            //ready up our client.
            return ph;
        }
        else if (auth.Contains(AuthTypes.SocksEncrypt))
        {
            //tell client that we chose socksboth.
            client.Send(new byte[] { (byte)HeaderTypes.Socks5, (byte)AuthTypes.SocksEncrypt });
            //wait for public key.
            SocksEncryption ph = new SocksEncryption();
            ph.GenerateKeys();
            //wait for public key.
            byte[] buffer = new byte[4096];
            int keysize = client.Receive(buffer, 0, buffer.Length);
            //store key in our encryption class.
            ph.SetKey(buffer, 0, keysize);
            //send key.
            client.Send(ph.GetPublicKey());
            //now we give them our key.
            client.Send(ph.ShareEncryptionKey());
            //send more.
            int enckeysize = client.Receive(buffer, 0, buffer.Length);
            //decrypt with our public key.
            byte[] newkey = new byte[enckeysize];
            Buffer.BlockCopy(buffer, 0, newkey, 0, enckeysize);
            ph.SetEncKey(ph.key.Decrypt(newkey, false));
            ph.SetType(AuthTypes.SocksEncrypt);
            //ready up our client.
            return ph;
        }
        else if (auth.Contains(AuthTypes.SocksCompress))
        {
            //start compression.
            client.Send(new byte[] { (byte)HeaderTypes.Socks5, (byte)AuthTypes.SocksCompress });
            SocksEncryption ph = new SocksEncryption();
            ph.SetType(AuthTypes.SocksCompress);
            //ready
        }
        else if (auth.Contains(AuthTypes.Login))
        {
            SocksEncryption ph = new SocksEncryption();
            ph.SetType(AuthTypes.Login);
            return ph;
        }

        return null;
    }

    public static User RequestLogin(SocksClient client)
    {
        //request authentication.
        client.Client.Send(new byte[] { (byte)HeaderTypes.Socks5, (byte)AuthTypes.Login });
        byte[] buff;
        int recv = Receive(client.Client, out buff);

        if (buff == null || buff[0] != 0x01)
        {
            return null;
        }

        int numusername = Convert.ToInt32(buff[1]);
        int numpassword = Convert.ToInt32(buff[(numusername + 2)]);
        string username = Encoding.ASCII.GetString(buff, 2, numusername);
        string password = Encoding.ASCII.GetString(buff, numusername + 3, numpassword);

        return new User(username, password, (IPEndPoint)client.Client.Sock.RemoteEndPoint);
    }

    public static SocksRequest? RequestTunnel(SocksClient client, SocksEncryption ph)
    {
        byte[] data;
        int recv = Receive(client.Client, out data);
        byte[] buff = ph.ProcessInputData(data, 0, recv);
        if (buff == null || (HeaderTypes)buff[0] != HeaderTypes.Socks5)
        {
            return null;
        }

        switch ((StreamTypes)buff[1])
        {
            case StreamTypes.Stream:
            {
                int fwd = 4;
                string address = "";
                switch ((AddressType)buff[3])
                {
                    case AddressType.IP:

                        {
                            for (int i = 4; i < 8; i++)
                            {
                                //grab IP.
                                address +=
                                    Convert.ToInt32(buff[i]).ToString() + (i != 7 ? "." : "");
                            }

                            fwd += 4;
                        }
                        break;
                    case AddressType.Domain:

                        {
                            int domainlen = Convert.ToInt32(buff[4]);
                            address += Encoding.ASCII.GetString(buff, 5, domainlen);
                            fwd += domainlen + 1;
                        }
                        break;
                    case AddressType.IPv6:
                        //can't handle IPV6 traffic just yet.
                        return null;
                }

                byte[] po = new byte[2];
                Array.Copy(buff, fwd, po, 0, 2);
                UInt16 port = BitConverter.ToUInt16(new byte[] { po[1], po[0] }, 0);
                return new SocksRequest(StreamTypes.Stream, (AddressType)buff[3], address, port);
            }
            default:
                //not supported.
                return null;
        }
    }

    public static int Receive(Client client, out byte[] buffer)
    {
        buffer = new byte[65535];
        return client.Receive(buffer, 0, buffer.Length);
    }
}
