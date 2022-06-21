using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CaptiveBrowser.ModelViews;
using ReactiveUI;

namespace CaptiveBrowser.Pages;

public sealed partial class Preparation
{
    private IDisposable? _uofWDisposable;

    public Preparation()
    {
        this.InitializeComponent();

        this.WhenActivated(
            (disposable) =>
            {
                this.OneWayBind(
                        ViewModel,
                        (vm) => vm.UnitsOfWork,
                        (v) => v.ItemsControl.ItemsSource
                    )
                    .DisposeWith(disposable);

                ViewModel
                    .WhenAnyValue(vm => vm.UnitsOfWork)
                    .Subscribe(
                        delegate(IImmutableList<UnitOfWorkModelView> list)
                        {
                            _uofWDisposable?.Dispose();

                            var dispose = new CompositeDisposable();

                            list.Select((i) => i.Changed)
                                .Merge()
                                .Subscribe(
                                    (_) =>
                                    {
                                        ForceListRefresh();
                                    }
                                )
                                .DisposeWith(dispose);

                            _uofWDisposable = dispose;

                            ForceListRefresh();
                        }
                    )
                    .DisposeWith(disposable);
            }
        );
    }

    private void ForceListRefresh()
    {
        ItemsControl.ItemTemplateSelector = ItemsControl.ItemTemplateSelector;
    }
}
