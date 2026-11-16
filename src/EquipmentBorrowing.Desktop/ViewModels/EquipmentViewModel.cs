using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class EquipmentViewModel : ObservableObject
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly EquipmentManagementService _equipmentService;

    [ObservableProperty]
    private ObservableCollection<Equipment> equipmentList = new();

    [ObservableProperty]
    private Equipment? selectedEquipment;

    [ObservableProperty]
    private bool showAvailableOnly;

    // Registration inputs (UC-15)
    [ObservableProperty]
    private string newIdentifier = string.Empty;

    [ObservableProperty]
    private string newName = string.Empty;

    [ObservableProperty]
    private string? newDescription;

    // Edit inputs (UC-16)
    [ObservableProperty]
    private string editIdentifier = string.Empty;

    [ObservableProperty]
    private string editName = string.Empty;

    [ObservableProperty]
    private string? editDescription;

    // Status & feedback
    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool isError;

    [ObservableProperty]
    private bool isLoading;

    public EquipmentViewModel(
        IEquipmentRepository equipmentRepository,
        EquipmentManagementService equipmentService)
    {
        _equipmentRepository = equipmentRepository;
        _equipmentService = equipmentService;
    }

    partial void OnShowAvailableOnlyChanged(bool value)
    {
        _ = LoadAsync();
    }

    partial void OnSelectedEquipmentChanged(Equipment? value)
    {
        if (value is not null)
        {
            EditIdentifier = value.EquipmentIdentifier;
            EditName = value.Name;
            EditDescription = value.Description ?? string.Empty;
        }
        else
        {
            EditIdentifier = string.Empty;
            EditName = string.Empty;
            EditDescription = string.Empty;
        }
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var items = ShowAvailableOnly
                ? await _equipmentRepository.GetAvailableAsync()
                : await _equipmentRepository.GetAllAsync();

            EquipmentList = new ObservableCollection<Equipment>(items);
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Failed to load equipment catalog: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RegisterEquipmentAsync()
    {
        StatusMessage = null;
        IsError = false;

        var result = await _equipmentService.RegisterEquipmentAsync(NewIdentifier, NewName, NewDescription);
        if (result.IsSuccess)
        {
            StatusMessage = result.FeedbackMessage;
            IsError = false;
            NewIdentifier = string.Empty;
            NewName = string.Empty;
            NewDescription = string.Empty;
            await LoadAsync();
        }
        else
        {
            StatusMessage = result.ErrorMessage;
            IsError = true;
        }
    }

    [RelayCommand]
    public async Task UpdateEquipmentAsync()
    {
        StatusMessage = null;
        IsError = false;

        if (SelectedEquipment is null)
        {
            StatusMessage = "Please select an equipment item to edit.";
            IsError = true;
            return;
        }

        var result = await _equipmentService.UpdateEquipmentAsync(
            SelectedEquipment.Id,
            EditIdentifier,
            EditName,
            EditDescription);

        if (result.IsSuccess)
        {
            StatusMessage = result.FeedbackMessage;
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