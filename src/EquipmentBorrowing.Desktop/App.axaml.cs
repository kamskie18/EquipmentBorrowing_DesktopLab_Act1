using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Desktop.ViewModels;
using EquipmentBorrowing.Desktop.Views;
using EquipmentBorrowing.Infrastructure.Persistence;
using EquipmentBorrowing.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System;

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
        using (var scope = provider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<EquipmentBorrowingDbContext>();
            dbContext.Database.Migrate();
            DbSeeder.Seed(dbContext);
        }

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
        var connectionString = DatabaseConfiguration.GetConnectionString();

        services.AddDbContext<EquipmentBorrowingDbContext>(options =>
            options.UseNpgsql(connectionString)
                   .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information));

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IStudentRepository, EfStudentRepository>();
        services.AddScoped<IEquipmentRepository, EfEquipmentRepository>();
        services.AddScoped<IBorrowingRepository, EfBorrowingRepository>();

        services.AddTransient<BorrowEquipmentService>();
        services.AddTransient<ReturnEquipmentService>();
        services.AddTransient<MultiItemBorrowService>();
        services.AddTransient<StudentManagementService>();
        services.AddTransient<EquipmentManagementService>();
        services.AddTransient<BorrowingHistoryService>();

        services.AddScoped<EquipmentViewModel>();
        services.AddScoped<StudentsViewModel>();
        services.AddScoped<CheckoutViewModel>();
        services.AddScoped<BorrowingsViewModel>();
        services.AddScoped<HistoryViewModel>();
        services.AddScoped<MainWindowViewModel>();

    }

}