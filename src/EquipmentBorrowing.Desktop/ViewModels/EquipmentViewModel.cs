using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Desktop.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class EquipmentViewModel : ObservableObject
{
    private readonly IEquipmentBorrowingApiClient _apiClient;

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

    public EquipmentViewModel(IEquipmentBorrowingApiClient apiClient)
    {
        _apiClient = apiClient;
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
            var dtos = await _apiClient.GetEquipmentAsync(ShowAvailableOnly ? true : null);
            var domainList = dtos.Select(d => new Equipment(d.Id, d.EquipmentCode, d.Name, d.Description, d.IsAvailable));
            EquipmentList = new ObservableCollection<Equipment>(domainList);
        }
        catch (ApiException ex)
        {
            IsError = true;
            StatusMessage = ex.IsConnectionFailure
                ? "Connection Error: API server is unreachable. Please verify http://localhost:5000 is running."
                : ex.IsServerError
                    ? "Server Error (500): The API encountered an internal server error."
                    : $"Error ({ex.StatusCode}): {ex.Message}";
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Unexpected error: {ex.Message}";
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

        try
        {
            var res = await _apiClient.RegisterEquipmentAsync(NewIdentifier, NewName, NewDescription);
            StatusMessage = $"Equipment '{res.Name}' ({res.EquipmentCode}) registered successfully.";
            IsError = false;
            NewIdentifier = string.Empty;
            NewName = string.Empty;
            NewDescription = string.Empty;
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            IsError = true;
            StatusMessage = ex.IsConnectionFailure
                ? "Connection Error: Unable to reach the API server."
                : ex.Message;
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Unexpected error: {ex.Message}";
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

        try
        {
            var res = await _apiClient.UpdateEquipmentAsync(
                SelectedEquipment.Id,
                EditIdentifier,
                EditName,
                EditDescription);

            StatusMessage = $"Equipment '{res.Name}' updated successfully.";
            IsError = false;
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            IsError = true;
            StatusMessage = ex.IsConnectionFailure
                ? "Connection Error: Unable to reach the API server."
                : ex.Message;
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Unexpected error: {ex.Message}";
        }
    }
}
