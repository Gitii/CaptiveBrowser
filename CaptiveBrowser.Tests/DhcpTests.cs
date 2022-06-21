using System.Globalization;
using CaptiveBrowser.Services;
using FluentAssertions;

namespace CaptiveBrowser.Tests;

public class DhcpTests
{
    static DhcpTests()
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    [Test]
    public async Task DiscoverDnsAsync()
    {
        var dhcp = new Dhcp();
        var networks = new NetworkInterfaceUtils();
        using var client = await networks.CreateClientAsync().ConfigureAwait(false);
        var address = await dhcp.DiscoverDnsServerAsync(client).ConfigureAwait(false);

        address.Should().NotBeEmpty();
    }
}
