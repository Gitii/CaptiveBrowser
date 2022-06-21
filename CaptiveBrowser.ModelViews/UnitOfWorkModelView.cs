using ReactiveUI;

namespace CaptiveBrowser.ModelViews;

public class UnitOfWorkModelView : ReactiveObject
{
    string _title;

    public UnitOfWorkModelView()
    {
        _title = String.Empty;
        _status = Status.InProgress;
        _statusMessage = String.Empty;
    }

    public string Title
    {
        get { return _title; }
        set { this.RaiseAndSetIfChanged(ref _title, value); }
    }

    string _statusMessage;

    public string StatusMessage
    {
        get { return _statusMessage; }
        set { this.RaiseAndSetIfChanged(ref _statusMessage, value); }
    }

    Status _status;

    public Status Status
    {
        get { return _status; }
        set { this.RaiseAndSetIfChanged(ref _status, value); }
    }
}

public enum Status
{
    InProgress = 0,
    Finished = 1,
    Failed = 2,
}
