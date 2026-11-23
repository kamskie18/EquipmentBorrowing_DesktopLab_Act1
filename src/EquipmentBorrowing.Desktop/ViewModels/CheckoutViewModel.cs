using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Desktop.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class EquipmentSelectionItem : ObservableObject
{
    public Equipment Equipment { get; }

    [ObservableProperty]
    private bool isSelected;

    public EquipmentSelectionItem(Equipment equipment)
    {
        Equipment = equipment;
    }
}

public partial class CheckoutViewModel : ObservableObject
{
    private readonly IEquipmentBorrowingApiClient _apiClient;

    [ObservableProperty]
    private ObservableCollection<Student> students = new();

    [ObservableProperty]
    private ObservableCollection<EquipmentSelectionItem> availableEquipment = new();

    [ObservableProperty]
    private Student? selectedStudent;

    [ObservableProperty]
    private DateTimeOffset expectedReturnDate = DateTimeOffset.Now.AddDays(7);

    [ObservableProperty]
    private bool simulateFailureSeam;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool isError;

    [ObservableProperty]
    private bool isLoading;

    public CheckoutViewModel(IEquipmentBorrowingApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var studentDtos = await _apiClient.GetStudentsAsync();
            var studentList = studentDtos.Select(s => new Student(s.Id, s.StudentNumber, s.FullName, s.Contact, s.IsEligible));
            Students = new ObservableCollection<Student>(studentList);

            var equipDtos = await _apiClient.GetEquipmentAsync(availableOnly: true);
            var equipList = equipDtos.Select(e => new Equipment(e.Id, e.EquipmentCode, e.Name, e.Description, e.IsAvailable));
            AvailableEquipment = new ObservableCollection<EquipmentSelectionItem>(
                equipList.Select(e => new EquipmentSelectionItem(e)));
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
            StatusMessage = $"Failed to load checkout options: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task BorrowSingleAsync()
    {
        StatusMessage = null;
        IsError = false;

        if (SelectedStudent is null)
        {
            StatusMessage = "Please select a student.";
            IsError = true;
            return;
        }

        var selectedItems = AvailableEquipment.Where(x => x.IsSelected).ToList();
        if (selectedItems.Count != 1)
        {
            StatusMessage = "Please select exactly one equipment item for single borrow.";
            IsError = true;
            return;
        }

        try
        {
            var dateStr = ExpectedReturnDate.DateTime.ToString("yyyy-MM-dd");
            var result = await _apiClient.BorrowEquipmentAsync(
                SelectedStudent.Id,
                selectedItems[0].Equipment.Id,
                dateStr);

            StatusMessage = $"Single item '{selectedItems[0].Equipment.Name}' borrowed successfully. Due: {result.ExpectedReturnDate}.";
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
            StatusMessage = $"Borrow failed: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task CheckoutMultipleAsync()
    {
        StatusMessage = null;
        IsError = false;

        if (SelectedStudent is null)
        {
            StatusMessage = "Please select a student.";
            IsError = true;
            return;
        }

        var selectedIds = AvailableEquipment
            .Where(x => x.IsSelected)
            .Select(x => x.Equipment.Id)
            .ToList();

        if (selectedIds.Count < 2)
        {
            StatusMessage = "Multi-item checkout requires selecting at least two equipment items.";
            IsError = true;
            return;
        }

        try
        {
            var dateStr = ExpectedReturnDate.DateTime.ToString("yyyy-MM-dd");
            var result = await _apiClient.CheckoutMultipleAsync(
                SelectedStudent.Id,
                selectedIds,
                dateStr);

            StatusMessage = $"Successfully checked out {result.Borrowings.Count} items to {SelectedStudent.Name}. Due: {dateStr}.";
            IsError = false;
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            IsError = true;
            StatusMessage = ex.IsConnectionFailure
                ? "Connection Error: Unable to reach the API server."
                : ex.Message;
            await LoadAsync(); // Reload to show atomic rollback / fresh availability
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Checkout failed: {ex.Message}";
            await LoadAsync();
        }
    }
}
