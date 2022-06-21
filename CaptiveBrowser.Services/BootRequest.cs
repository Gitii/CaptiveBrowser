using System.Net;
using System.Net.NetworkInformation;
using DhcpDotNet;

namespace CaptiveBrowser.Services;

public record class BootRequest
{
    public const int HEADER = 1;

    public BootRequest()
    {
        Mac = PhysicalAddress.None;
        TransactionId = Array.Empty<byte>();
    }

    public PhysicalAddress Mac { get; init; }

    public byte[] TransactionId { get; init; }

    public byte[] Build()
    {
        // Create optional payload bytes that can be added to the main payload.
        var dhcpMessageTypeOption = new DHCPv4Option()
        {
            optionId = DHCPv4OptionIds.DhcpMessageType,
            optionLength = 0x01,
            optionValue = new byte[] { 0x01 },
        };

        var address = IPAddress.Any;
        var addressBytes = address.GetAddressBytes();

        var macData = Mac.GetAddressBytes();
        var dhcpDiscoveryPacket = new DHCPv4Packet()
        {
            op = HEADER,
            htype = 1,
            hlen = (byte)macData.Length,
            hops = 0,
            xid = TransactionId,
            secs = Array.Empty<byte>(),
            flags = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(0x8000)),
            chaddr = macData,
            giaddr = addressBytes,
            dhcpOptions = dhcpMessageTypeOption.buildDhcpOption().ToArray(),
        };

        return dhcpDiscoveryPacket.buildPacket();
    }
}
