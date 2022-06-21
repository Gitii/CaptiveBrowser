using System;
using System.Threading.Tasks;
using CaptiveBrowser.ModelViews;
using CaptiveBrowser.Pages;
using CaptiveBrowser.Services;
using Community.Sextant.WinUI.Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;

namespace CaptiveBrowser;

public static class Program
{
    [STAThread]
    public static Task Main(string[] args)
    {
        var builder = new WindowsAppSdkHostBuilder<App>();

        ConfigureServices(builder);

        var app = builder.Build();

        app.Services.UseMicrosoftDependencyResolver();

        return app.StartAsync();
    }

    private static void ConfigureServices(WindowsAppSdkHostBuilder<App> builder)
    {
        builder.ConfigureServices(
            (context, collection) =>
            {
                ConfigureSplatIntegration(collection);
                ConfigureModelViews(collection);
                ConfigureComplexServices(collection);
                ConfigureServiceDiscovery(collection);
            }
        );
    }

    private static void ConfigureServiceDiscovery(IServiceCollection collection)
    {
        collection.Scan(
            scan =>
                scan
                // We start out with all types in the assembly of ITransientService
                .FromAssembliesOf(typeof(PreparationModelView))
                    .AddClasses(true)
                    .AsImplementedInterfaces()
                    .AsSelf()
                    .WithTransientLifetime()
                    .AddClasses((classes) => classes.AssignableTo<ReactiveObject>())
                    .AsSelf()
        );

        collection.Scan(
            scan =>
                scan
                // We start out with all types in the assembly of ITransientService
                .FromAssembliesOf(typeof(IDhcp))
                    .AddClasses(true)
                    .AsImplementedInterfaces()
                    .AsSelf()
                    .WithTransientLifetime()
        );
    }

    private static void ConfigureComplexServices(IServiceCollection collection)
    {
        collection.AddSingleton<MainWindow>();
    }

    private static void ConfigureModelViews(IServiceCollection collection)
    {
        collection.UseSextant(
            builder =>
            {
                builder.ConfigureDefaults();
                builder.ConfigureViews(
                    viewBuilder =>
                    {
                        viewBuilder.RegisterViewAndViewModel<Preparation, PreparationModelView>();
                    }
                );
            }
        );
    }

    private static void ConfigureSplatIntegration(IServiceCollection collection)
    {
        collection.UseMicrosoftDependencyResolver();
        var resolver = Locator.CurrentMutable;
        resolver.InitializeSplat();
        resolver.InitializeReactiveUI(RegistrationNamespace.WinUI);
    }
}
