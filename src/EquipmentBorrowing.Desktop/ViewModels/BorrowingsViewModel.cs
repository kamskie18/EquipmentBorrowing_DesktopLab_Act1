using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class BorrowingsViewModel : ObservableObject
{
    private readonly IBorrowingRepository _borrowingRepository;
    private readonly ReturnEquipmentService _returnEquipmentService;

    [ObservableProperty]
    private ObservableCollection<Borrowing> activeBorrowings = new();

    [ObservableProperty]
    private Borrowing? selectedBorrowing;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool isError;

    [ObservableProperty]
    private bool isLoading;

    public BorrowingsViewModel(IBorrowingRepository borrowingRepository, ReturnEquipmentService returnEquipmentService)
    {
        _borrowingRepository = borrowingRepository;
        _returnEquipmentService = returnEquipmentService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var borrowings = await _borrowingRepository.GetActiveAsync();
            ActiveBorrowings = new ObservableCollection<Borrowing>(borrowings);
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Failed to load active borrowings: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ReturnAsync()
    {
        StatusMessage = null;
        IsError = false;

        if (SelectedBorrowing is null)
        {
            StatusMessage = "Please select a borrowing to return.";
            IsError = true;
            return;
        }

        var result = await _returnEquipmentService.ReturnAsync(SelectedBorrowing.Id, DateTime.Now);

        if (result.IsSuccess)
        {
            StatusMessage = "Equipment returned successfully. History retained and equipment is available.";
            IsError = false;
            await LoadAsync();
        }
        else
        {
            StatusMessage = result.ErrorMessage;
            IsError = true;
        }
    }
}