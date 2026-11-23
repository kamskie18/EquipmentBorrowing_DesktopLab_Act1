using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EquipmentBorrowing.Desktop.Services;
using EquipmentBorrowing.Desktop.ViewModels;
using EquipmentBorrowing.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace EquipmentBorrowing.Desktop;

public partial class App : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        var provider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindowViewModel = provider.GetRequiredService<MainWindowViewModel>();

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(ServiceCollection services)
    {
        // Configure API Client (HTTP only, no direct database or SQL in desktop app)
        var apiBaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://localhost:5000";

        services.AddSingleton<HttpClient>(sp =>
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(apiBaseUrl),
                Timeout = TimeSpan.FromSeconds(10)
            };
            return client;
        });

        services.AddSingleton<IEquipmentBorrowingApiClient, EquipmentBorrowingApiClient>();

        // Register ViewModels (Singleton for rock-solid UI state persistence across navigation)
        services.AddSingleton<EquipmentViewModel>();
        services.AddSingleton<StudentsViewModel>();
        services.AddSingleton<CheckoutViewModel>();
        services.AddSingleton<BorrowingsViewModel>();
        services.AddSingleton<HistoryViewModel>();
        services.AddSingleton<MainWindowViewModel>();
    }
}
