using Windows.Foundation;

namespace CaptiveBrowser.ModelViews;

public interface IViewLayoutOptions
{
    public Size? DesiredViewSize { get; }

    public bool HideTitleBar { get; }
}
