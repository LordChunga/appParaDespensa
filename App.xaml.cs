#nullable enable

using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PosLocal.Data;
using PosLocal.Services;
using PosLocal.ViewModels;
using PosLocal.Views;

namespace PosLocal;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);

        _serviceProvider = services.BuildServiceProvider();
        _serviceProvider.GetRequiredService<IPosDbContext>()
            .InitializeAsync()
            .GetAwaiter()
            .GetResult();
        _serviceProvider.GetRequiredService<IDatabaseSeederService>()
            .SeedAsync()
            .GetAwaiter()
            .GetResult();

        var mainWindow = _serviceProvider.GetRequiredService<PuntoVentaView>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    public void NavigateTo<TWindow>(Window currentWindow)
        where TWindow : Window
    {
        if (currentWindow is TWindow)
        {
            return;
        }

        if (_serviceProvider is null)
        {
            throw new InvalidOperationException("La navegacion no esta disponible antes de inicializar la aplicacion.");
        }

        var nextWindow = _serviceProvider.GetRequiredService<TWindow>();
        nextWindow.WindowStartupLocation = WindowStartupLocation.Manual;
        nextWindow.Left = currentWindow.Left;
        nextWindow.Top = currentWindow.Top;
        nextWindow.Width = currentWindow.Width;
        nextWindow.Height = currentWindow.Height;
        nextWindow.WindowState = currentWindow.WindowState == WindowState.Minimized
            ? WindowState.Normal
            : currentWindow.WindowState;

        MainWindow = nextWindow;
        nextWindow.Show();
        currentWindow.Close();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IPosDbContext>(_ =>
        {
            var dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DespensaIsabel");

            return new PosDbContext(Path.Combine(dataDirectory, "pos-local.db"));
        });

        services.AddSingleton<IPosProductoService, PosProductoService>();
        services.AddSingleton<IPosVentaService, PosVentaService>();
        services.AddSingleton<IPosDialogService, PosDialogService>();
        services.AddSingleton<IDatabaseSeederService, DatabaseSeederService>();
        services.AddSingleton<IAppNavigationService, AppNavigationService>();

        services.AddSingleton<ICompraService, CompraService>();
        services.AddSingleton<ICompraDialogService, CompraDialogService>();

        services.AddSingleton<IProductoCatalogoService, ProductoCatalogoService>();
        services.AddSingleton<IProductoCatalogoDialogService, ProductoCatalogoDialogService>();

        services.AddSingleton<IInventarioService, InventarioService>();
        services.AddSingleton<IInventarioDialogService, InventarioDialogService>();

        services.AddSingleton<IReportesService, ReportesService>();
        services.AddSingleton<IReportesDialogService, ReportesDialogService>();
        services.AddSingleton<IAppSettingsService>(_ =>
        {
            var dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DespensaIsabel");

            return new AppSettingsService(Path.Combine(dataDirectory, "appsettings.json"));
        });

        services.AddTransient<PuntoVentaViewModel>();
        services.AddTransient<ComprasViewModel>();
        services.AddTransient<ProductosViewModel>();
        services.AddTransient<InventarioProductosViewModel>();
        services.AddTransient<ReportesViewModel>();

        services.AddTransient<PuntoVentaView>(provider => new PuntoVentaView
        {
            DataContext = provider.GetRequiredService<PuntoVentaViewModel>()
        });

        services.AddTransient<ComprasView>(provider => new ComprasView
        {
            DataContext = provider.GetRequiredService<ComprasViewModel>()
        });

        services.AddTransient<ProductosView>(provider => new ProductosView
        {
            DataContext = provider.GetRequiredService<ProductosViewModel>()
        });

        services.AddTransient<InventarioProductosView>(provider => new InventarioProductosView
        {
            DataContext = provider.GetRequiredService<InventarioProductosViewModel>()
        });

        services.AddTransient<ReportesView>(provider => new ReportesView
        {
            DataContext = provider.GetRequiredService<ReportesViewModel>()
        });
    }
}
