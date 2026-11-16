using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class StudentsViewModel : ObservableObject
{
    private readonly IStudentRepository _studentRepository;
    private readonly StudentManagementService _studentService;

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

    public StudentsViewModel(IStudentRepository studentRepository, StudentManagementService studentService)
    {
        _studentRepository = studentRepository;
        _studentService = studentService;
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
            var list = await _studentRepository.GetAllAsync();
            Students = new ObservableCollection<Student>(list);
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Failed to load students: {ex.Message}";
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

        var result = await _studentService.RegisterStudentAsync(NewStudentNumber, NewName, NewContactInfo);
        if (result.IsSuccess)
        {
            StatusMessage = result.FeedbackMessage;
            IsError = false;
            NewStudentNumber = string.Empty;
            NewName = string.Empty;
            NewContactInfo = string.Empty;
            await LoadAsync();
        }
        else
        {
            StatusMessage = result.ErrorMessage;
            IsError = true;
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

        var result = await _studentService.UpdateStudentAsync(
            SelectedStudent.Id,
            EditStudentNumber,
            EditName,
            EditContactInfo);

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

        var result = await _studentService.ChangeEligibilityAsync(SelectedStudent.Id, permit);
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
