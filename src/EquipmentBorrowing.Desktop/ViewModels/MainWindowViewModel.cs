using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly EquipmentViewModel _equipmentViewModel;
    private readonly StudentsViewModel _studentsViewModel;
    private readonly CheckoutViewModel _checkoutViewModel;
    private readonly BorrowingsViewModel _borrowingsViewModel;
    private readonly HistoryViewModel _historyViewModel;

    [ObservableProperty]
    private ObservableObject? currentView;

    public MainWindowViewModel(
        EquipmentViewModel equipmentViewModel,
        StudentsViewModel studentsViewModel,
        CheckoutViewModel checkoutViewModel,
        BorrowingsViewModel borrowingsViewModel,
        HistoryViewModel historyViewModel)
    {
        _equipmentViewModel = equipmentViewModel;
        _studentsViewModel = studentsViewModel;
        _checkoutViewModel = checkoutViewModel;
        _borrowingsViewModel = borrowingsViewModel;
        _historyViewModel = historyViewModel;

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
}