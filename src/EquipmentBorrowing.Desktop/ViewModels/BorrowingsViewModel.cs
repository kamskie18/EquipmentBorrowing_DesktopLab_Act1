using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Desktop.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class BorrowingsViewModel : ObservableObject
{
    private readonly IEquipmentBorrowingApiClient _apiClient;

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

    public BorrowingsViewModel(IEquipmentBorrowingApiClient apiClient)
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
            var dtos = await _apiClient.GetBorrowingsAsync(status: "Active");
            var domainList = dtos.Select(d =>
            {
                var b = new Borrowing(
                    d.Id,
                    d.StudentId,
                    d.EquipmentId,
                    DateTime.Parse(d.BorrowedDate),
                    DateTime.Parse(d.ExpectedReturnDate)
                );
                // attach shallow student/equipment for UI data binding
                if (!string.IsNullOrEmpty(d.StudentName))
                {
                    typeof(Borrowing).GetProperty("Student")?.SetValue(b, new Student(d.StudentId, d.StudentNumber ?? "", d.StudentName, null, true));
                }
                if (!string.IsNullOrEmpty(d.EquipmentName))
                {
                    typeof(Borrowing).GetProperty("Equipment")?.SetValue(b, new Equipment(d.EquipmentId, d.EquipmentCode ?? "", d.EquipmentName, null, false));
                }
                return b;
            });

            ActiveBorrowings = new ObservableCollection<Borrowing>(domainList);
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
            StatusMessage = "Please select an active borrowing to return.";
            IsError = true;
            return;
        }

        try
        {
            var res = await _apiClient.ReturnBorrowingAsync(SelectedBorrowing.Id);
            StatusMessage = $"Equipment return confirmed successfully. Item is now available.";
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
            StatusMessage = $"Return failed: {ex.Message}";
        }
    }
}
