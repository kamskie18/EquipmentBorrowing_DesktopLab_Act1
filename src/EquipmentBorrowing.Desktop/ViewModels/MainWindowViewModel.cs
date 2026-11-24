using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Desktop.Services;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly EquipmentViewModel _equipmentViewModel;
    private readonly StudentsViewModel _studentsViewModel;
    private readonly CheckoutViewModel _checkoutViewModel;
    private readonly BorrowingsViewModel _borrowingsViewModel;
    private readonly HistoryViewModel _historyViewModel;
    private readonly IEquipmentBorrowingApiClient _apiClient;

    [ObservableProperty]
    private ObservableObject? currentView;

    [ObservableProperty]
    private string? statusMessage;

    public MainWindowViewModel(
        EquipmentViewModel equipmentViewModel,
        StudentsViewModel studentsViewModel,
        CheckoutViewModel checkoutViewModel,
        BorrowingsViewModel borrowingsViewModel,
        HistoryViewModel historyViewModel,
        IEquipmentBorrowingApiClient apiClient)
    {
        _equipmentViewModel = equipmentViewModel;
        _studentsViewModel = studentsViewModel;
        _checkoutViewModel = checkoutViewModel;
        _borrowingsViewModel = borrowingsViewModel;
        _historyViewModel = historyViewModel;
        _apiClient = apiClient;

        CurrentView = _equipmentViewModel;
        _ = _equipmentViewModel.LoadCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task ShowEquipmentAsync()
    {
        CurrentView = _equipmentViewModel;
        await _equipmentViewModel.LoadCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task ShowStudentsAsync()
    {
        CurrentView = _studentsViewModel;
        await _studentsViewModel.LoadCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task ShowCheckoutAsync()
    {
        CurrentView = _checkoutViewModel;
        await _checkoutViewModel.LoadCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task ShowBorrowingsAsync()
    {
        CurrentView = _borrowingsViewModel;
        await _borrowingsViewModel.LoadCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task ShowHistoryAsync()
    {
        CurrentView = _historyViewModel;
        await _historyViewModel.LoadCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task ResetTestDataAsync()
    {
        StatusMessage = "Resetting test dataset...";
        try
        {
            var msg = await _apiClient.ResetSeedDataAsync();
            StatusMessage = "Dataset reset to S1-S4 & E1-E7.";

            // Reload current view
            if (CurrentView == _equipmentViewModel) await _equipmentViewModel.LoadCommand.ExecuteAsync(null);
            else if (CurrentView == _studentsViewModel) await _studentsViewModel.LoadCommand.ExecuteAsync(null);
            else if (CurrentView == _checkoutViewModel) await _checkoutViewModel.LoadCommand.ExecuteAsync(null);
            else if (CurrentView == _borrowingsViewModel) await _borrowingsViewModel.LoadCommand.ExecuteAsync(null);
            else if (CurrentView == _historyViewModel) await _historyViewModel.LoadCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Reset failed: {ex.Message}";
        }
    }
}
