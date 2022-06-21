using System.Net;
using System.Net.NetworkInformation;
using DhcpDotNet;

namespace CaptiveBrowser.Services;

public class Dhcp : IDhcp
{
    public async Task<string> DiscoverDnsServerAsync(Client client)
    {
        (byte[] data, byte[] id) = BuildBootDiscoverPacket(client.Mac);

        var sendTask = client.Udp.SendAsync(data, new IPEndPoint(IPAddress.Broadcast, 67));

        DHCPv4Packet response;
        do
        {
            var rawResponse = await client.Udp.ReceiveAsync().ConfigureAwait(false);

            await sendTask.ConfigureAwait(false);

            response = new DHCPv4Packet();
            response.parsePacket(rawResponse.Buffer);
        } while (!response.xid.SequenceEqual(id));

        return ParseBootReply(response).DnsServerAddress;
    }

    private BootReply ParseBootReply(DHCPv4Packet response)
    {
        DHCPv4Option opt = new DHCPv4Option();
        var options = opt.parseDhcpOptions(response.dhcpOptions);

        var dnsServerAddressData = options
            .First((o) => ((DHCPv4OptionIds)o.optionIdBytes) == DHCPv4OptionIds.DomainNameServer)
            .optionValue;

        return new BootReply()
        {
            DnsServerAddress = new IPAddress(dnsServerAddressData).ToString(),
        };
    }

    private (byte[] data, byte[] id) BuildBootDiscoverPacket(PhysicalAddress mac)
    {
        var request = new BootRequest() { TransactionId = GenerateId(), Mac = mac, };

        return (request.Build(), request.TransactionId);
    }

    private byte[] GenerateId()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        Random.Shared.NextBytes(data);

        return data;
    }
}
