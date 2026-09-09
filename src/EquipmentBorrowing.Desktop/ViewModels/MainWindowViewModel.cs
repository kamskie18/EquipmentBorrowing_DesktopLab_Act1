using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly EquipmentViewModel _equipmentViewModel;
    private readonly BorrowingsViewModel _borrowingsViewModel;

    [ObservableProperty]
    private ObservableObject? currentView;

    public MainWindowViewModel(EquipmentViewModel equipmentViewModel, BorrowingsViewModel borrowingsViewModel)
    {
        _equipmentViewModel = equipmentViewModel;
        _borrowingsViewModel = borrowingsViewModel;
        CurrentView = _equipmentViewModel;

        _ = _equipmentViewModel.LoadCommand.ExecuteAsync(null);
        _ = _borrowingsViewModel.LoadCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task ShowEquipmentAsync()
    {
        CurrentView = _equipmentViewModel;
        await _equipmentViewModel.LoadCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task ShowBorrowingsAsync()
    {
        CurrentView = _borrowingsViewModel;
        await _borrowingsViewModel.LoadCommand.ExecuteAsync(null);
    }
}