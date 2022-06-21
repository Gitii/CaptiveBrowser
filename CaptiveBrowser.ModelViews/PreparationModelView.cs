using System.Collections.Immutable;
using System.Net;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Windows.Foundation;
using CaptiveBrowser.Services;
using ReactiveUI;
using Sextant;

namespace CaptiveBrowser.ModelViews;

public class PreparationModelView : ReactiveObject, INavigable, IViewLayoutOptions
{
    public string Id { get; } = nameof(PreparationModelView);

    private IDhcp _dhcp;
    private INetworkInterfaceUtils _interfaceUtils;
    IImmutableList<UnitOfWorkModelView> _unitsOfWork;

    public IImmutableList<UnitOfWorkModelView> UnitsOfWork
    {
        get { return _unitsOfWork; }
        set { this.RaiseAndSetIfChanged(ref _unitsOfWork, value); }
    }

    public PreparationModelView(INetworkInterfaceUtils interfaceUtils, IDhcp dhcp)
    {
        _interfaceUtils = interfaceUtils;
        _dhcp = dhcp;
        _unitsOfWork = ImmutableList<UnitOfWorkModelView>.Empty;
    }

    public async Task PrepareAsync()
    {
        var captivePortalDetected = await StartUnitOfWorkAsync(
                "Captive Portal?",
                DetectCaptivePortal
            )
            .ConfigureAwait(true);

        if (!captivePortalDetected)
        {
            return;
        }

        var client = await StartUnitOfWorkAsync("Network interface", DetermineNetWorkInterfaceAsync)
            .ConfigureAwait(true);

        await StartUnitOfWorkAsync("DNS server", DetermineDnsServerAsync, client)
            .ConfigureAwait(true);
    }

    private async Task<bool> DetectCaptivePortal(IProgress<string> progress)
    {
        var httpClient = new HttpClient();
        try
        {
            var response = await httpClient.GetAsync(
                "http://connectivitycheck.android.com/generate_204"
            );

            var internetIsReachable = response.StatusCode == HttpStatusCode.NoContent;
            progress.Report(internetIsReachable ? "No, internet is reachable." : "Yes");

            return !internetIsReachable;
        }
        catch (Exception e)
        {
            progress.Report("High probability");
            return true;
        }
        finally
        {
            httpClient.Dispose();
        }
    }

    private async Task<string> DetermineDnsServerAsync(IProgress<string> progress, Client client)
    {
        var dnsServer = await _dhcp.DiscoverDnsServerAsync(client).ConfigureAwait(false);

        progress.Report(dnsServer);

        return dnsServer;
    }

    private async Task<Client> DetermineNetWorkInterfaceAsync(IProgress<string> progress)
    {
        var client = await _interfaceUtils.CreateClientAsync().ConfigureAwait(false);

        progress.Report(
            $"Interface: {client.InterfaceName}\nIP-address: {client.IpAddress}\nMAC: {client.Mac}"
        );

        return client;
    }

    private Task<T> StartUnitOfWorkAsync<T, K>(
        string title,
        Func<IProgress<string>, K, Task<T>> worker,
        K arg1
    )
    {
        return StartUnitOfWorkAsync<T>(title, progress => worker(progress, arg1));
    }

    private async Task<T> StartUnitOfWorkAsync<T>(
        string title,
        Func<IProgress<string>, Task<T>> worker
    )
    {
        var uof = new UnitOfWorkModelView { Title = title };
        var progress = new Progress<string>(
            (message) => RxApp.MainThreadScheduler.Schedule(() => uof.StatusMessage = message)
        );

        UnitsOfWork = UnitsOfWork.Add(uof);

        T result;
        try
        {
            result = await worker(progress).ConfigureAwait(true);
        }
        catch (Exception e)
        {
            uof.Status = Status.Failed;
            uof.StatusMessage = e.Message;

            throw;
        }

        uof.Status = Status.Finished;

        return result;
    }

    public IObservable<Unit> WhenNavigatedTo(INavigationParameter parameter)
    {
        return PrepareAsync().ToObservable(RxApp.MainThreadScheduler);
    }

    public IObservable<Unit> WhenNavigatedFrom(INavigationParameter parameter)
    {
        return Observable.Empty<Unit>();
    }

    public IObservable<Unit> WhenNavigatingTo(INavigationParameter parameter)
    {
        return Observable.Empty<Unit>();
    }

    public Size? DesiredViewSize { get; } = new Size(600, 400);
    public bool HideTitleBar { get; } = true;
}
