using System.Net;

namespace CaptiveBrowser.Services;

internal interface IProxy : IDisposable
{
    Task<IPEndPoint> StartAsync(IPEndPoint proxy);
    void Stop();
    void Dispose();
}
