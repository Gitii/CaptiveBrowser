using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reactive;
using System.Threading;
using Windows.Graphics;
using CaptiveBrowser.ModelViews;
using Community.Sextant.WinUI;
using Community.Sextant.WinUI.Adapters;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using Sextant;
using WinRT;
using WinRT.Interop;

namespace CaptiveBrowser;

public sealed partial class MainWindow
{
    private readonly IParameterViewStackService _viewStackService;
    private readonly INavigationService _navigationService;
    private readonly AppWindow _appWindow;
    private readonly OverlappedPresenter _presenter;

    public MainWindow(
        IParameterViewStackService viewStackService,
        INavigationService navigationService
    )
    {
        _viewStackService = viewStackService;
        _navigationService = navigationService;

        this.InitializeComponent();

        (_appWindow, _presenter) = GetAppWindowForCurrentWindow();

        TrySetMicaBackdrop();

        _presenter.IsMaximizable = false;
        _presenter.IsMinimizable = false;
        _presenter.IsResizable = false;
        _appWindow.ResizeClient(new SizeInt32(600, 400));
    }

    private void MainWindow_OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (_configurationSource != null)
        {
            _configurationSource.IsInputActive =
                args.WindowActivationState != WindowActivationState.Deactivated;
        }

        if (ContentControl.Content != null)
        {
            return;
        }

        var uiContext =
            SynchronizationContext.Current ?? throw new Exception("UI Context is null!");

        RxApp.DefaultExceptionHandler = Observer
            .Create<Exception>(
                (ex) =>
                {
                    uiContext.Post(
                        (_) =>
                        {
                            if (Debugger.IsAttached)
                            {
                                Debugger.Break();
                            }

                            Console.Error.WriteLine(ex);
                        },
                        null
                    );
                }
            )
            .NotifyOn(RxApp.MainThreadScheduler);

        var content = new Frame();

        _navigationService.SetAdapter(new FrameNavigationViewAdapter(content, this));

        _viewStackService.PageStack.Subscribe(OnStackChanged);

        ContentControl.Content = content;

        _viewStackService.PushPage<PreparationModelView>(new NavigationParameter()).Subscribe();
    }

    private void OnStackChanged(IImmutableList<IViewModel> obj)
    {
        if (obj.Count > 0 && obj[0] is IViewLayoutOptions viewLayoutOptions)
        {
            if (viewLayoutOptions.DesiredViewSize.HasValue)
            {
                _appWindow.ResizeClient(
                    new SizeInt32(
                        (int)viewLayoutOptions.DesiredViewSize.Value.Width,
                        (int)viewLayoutOptions.DesiredViewSize.Value.Height
                    )
                );
            }

            if (viewLayoutOptions.HideTitleBar)
            {
                ExtendsContentIntoTitleBar = true;
                SetTitleBar(ContentControl);
            }
            else
            {
                ExtendsContentIntoTitleBar = false;
                SetTitleBar(null);
            }
        }
    }

    private void CloseButton_OnClick(object sender, WindowEventArgs e)
    {
        Environment.Exit(0);
    }

    WindowsSystemDispatcherQueueHelper? _wsdqHelper; // See separate sample below for implementation
    Microsoft.UI.Composition.SystemBackdrops.MicaController? _micaController;
    Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration? _configurationSource;

    bool TrySetMicaBackdrop()
    {
        if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
        {
            _wsdqHelper = new WindowsSystemDispatcherQueueHelper();
            _wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

            // Hooking up the policy object
            _configurationSource =
                new Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration();
            this.Closed += MicaCleanup;
            ((FrameworkElement)this.Content).ActualThemeChanged += Window_ThemeChanged;

            // Initial configuration state.
            _configurationSource.IsInputActive = true;
            SetConfigurationSourceTheme();

            _micaController = new Microsoft.UI.Composition.SystemBackdrops.MicaController();

            // Enable the system backdrop.
            // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
            _micaController.AddSystemBackdropTarget(
                this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>()
            );
            _micaController.SetSystemBackdropConfiguration(_configurationSource);
            return true; // succeeded
        }

        return false; // Mica is not supported on this system
    }

    private void MicaCleanup(object sender, WindowEventArgs args)
    {
        // Make sure any Mica/Acrylic controller is disposed so it doesn't try to
        // use this closed window.
        if (_micaController != null)
        {
            _micaController.Dispose();
            _micaController = null;
        }

        _configurationSource = null;
    }

    private void Window_ThemeChanged(FrameworkElement sender, object args)
    {
        if (_configurationSource != null)
        {
            SetConfigurationSourceTheme();
        }
    }

    private void SetConfigurationSourceTheme()
    {
        if (_configurationSource == null)
        {
            return;
        }

        switch (((FrameworkElement)this.Content).ActualTheme)
        {
            case ElementTheme.Dark:
                _configurationSource.Theme = Microsoft
                    .UI
                    .Composition
                    .SystemBackdrops
                    .SystemBackdropTheme
                    .Dark;
                break;
            case ElementTheme.Light:
                _configurationSource.Theme = Microsoft
                    .UI
                    .Composition
                    .SystemBackdrops
                    .SystemBackdropTheme
                    .Light;
                break;
            case ElementTheme.Default:
                _configurationSource.Theme = Microsoft
                    .UI
                    .Composition
                    .SystemBackdrops
                    .SystemBackdropTheme
                    .Default;
                break;
        }
    }

    private (AppWindow, OverlappedPresenter) GetAppWindowForCurrentWindow()
    {
        IntPtr hWnd = WindowNative.GetWindowHandle(this);
        WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var w = AppWindow.GetFromWindowId(wndId);
        var p = (OverlappedPresenter)w.Presenter;

        return (w, p);
    }
}
