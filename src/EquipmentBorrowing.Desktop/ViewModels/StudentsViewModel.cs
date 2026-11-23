using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Desktop.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class StudentsViewModel : ObservableObject
{
    private readonly IEquipmentBorrowingApiClient _apiClient;

    [ObservableProperty]
    private ObservableCollection<Student> students = new();

    [ObservableProperty]
    private Student? selectedStudent;

    // Registration inputs (UC-12)
    [ObservableProperty]
    private string newStudentNumber = string.Empty;

    [ObservableProperty]
    private string newName = string.Empty;

    [ObservableProperty]
    private string? newContactInfo;

    // Edit inputs (UC-13)
    [ObservableProperty]
    private string editStudentNumber = string.Empty;

    [ObservableProperty]
    private string editName = string.Empty;

    [ObservableProperty]
    private string? editContactInfo;

    // Status & state feedback (BR-18, BR-19)
    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool isError;

    [ObservableProperty]
    private bool isLoading;

    public StudentsViewModel(IEquipmentBorrowingApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    partial void OnSelectedStudentChanged(Student? value)
    {
        if (value is not null)
        {
            EditStudentNumber = value.StudentNumber;
            EditName = value.Name;
            EditContactInfo = value.ContactInfo ?? string.Empty;
        }
        else
        {
            EditStudentNumber = string.Empty;
            EditName = string.Empty;
            EditContactInfo = string.Empty;
        }
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var dtos = await _apiClient.GetStudentsAsync();
            var domainList = dtos.Select(d => new Student(d.Id, d.StudentNumber, d.FullName, d.Contact, d.IsEligible));
            Students = new ObservableCollection<Student>(domainList);
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
    public async Task RegisterStudentAsync()
    {
        StatusMessage = null;
        IsError = false;

        try
        {
            var res = await _apiClient.RegisterStudentAsync(NewStudentNumber, NewName, NewContactInfo);
            StatusMessage = $"Student '{res.FullName}' ({res.StudentNumber}) registered successfully.";
            IsError = false;
            NewStudentNumber = string.Empty;
            NewName = string.Empty;
            NewContactInfo = string.Empty;
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
    public async Task UpdateStudentAsync()
    {
        StatusMessage = null;
        IsError = false;

        if (SelectedStudent is null)
        {
            StatusMessage = "Please select a student to edit.";
            IsError = true;
            return;
        }

        try
        {
            var res = await _apiClient.UpdateStudentAsync(
                SelectedStudent.Id,
                EditStudentNumber,
                EditName,
                EditContactInfo);

            StatusMessage = $"Student '{res.FullName}' updated successfully.";
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

    [RelayCommand]
    public async Task PermitStudentAsync()
    {
        await SetEligibilityAsync(permit: true);
    }

    [RelayCommand]
    public async Task RestrictStudentAsync()
    {
        await SetEligibilityAsync(permit: false);
    }

    private async Task SetEligibilityAsync(bool permit)
    {
        StatusMessage = null;
        IsError = false;

        if (SelectedStudent is null)
        {
            StatusMessage = "Please select a student first.";
            IsError = true;
            return;
        }

        try
        {
            var res = await _apiClient.UpdateStudentEligibilityAsync(SelectedStudent.Id, permit);
            StatusMessage = permit
                ? $"Student '{res.FullName}' is now permitted to borrow equipment."
                : $"Student '{res.FullName}' is now restricted from borrowing equipment.";
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
