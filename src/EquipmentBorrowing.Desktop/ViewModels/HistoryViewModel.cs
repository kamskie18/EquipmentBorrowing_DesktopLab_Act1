using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly BorrowingHistoryService _historyService;
    private readonly IStudentRepository _studentRepository;
    private readonly IEquipmentRepository _equipmentRepository;

    // View sub-mode: 0 = Global Search/Filter/Sort, 1 = By Student, 2 = By Equipment
    [ObservableProperty]
    private int selectedTab = 0;

    // 1. Global Search/Filter/Sort (UC-09)
    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private BorrowingStatusFilter selectedStatusFilter = BorrowingStatusFilter.All;

    [ObservableProperty]
    private BorrowingSortField selectedSortField = BorrowingSortField.CheckoutDate;

    [ObservableProperty]
    private bool isSortDescending = true;

    [ObservableProperty]
    private ObservableCollection<Borrowing> globalResults = new();

    // 2. Student History (UC-07)
    [ObservableProperty]
    private ObservableCollection<Student> allStudents = new();

    [ObservableProperty]
    private Student? selectedStudent;

    [ObservableProperty]
    private ObservableCollection<Borrowing> studentHistory = new();

    // 3. Equipment History (UC-08)
    [ObservableProperty]
    private ObservableCollection<Equipment> allEquipment = new();

    [ObservableProperty]
    private Equipment? selectedEquipment;

    [ObservableProperty]
    private ObservableCollection<Borrowing> equipmentHistory = new();

    // States & Feedback (BR-18, BR-19)
    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool isError;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isEmpty;

    public HistoryViewModel(
        BorrowingHistoryService historyService,
        IStudentRepository studentRepository,
        IEquipmentRepository equipmentRepository)
    {
        _historyService = historyService;
        _studentRepository = studentRepository;
        _equipmentRepository = equipmentRepository;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var students = await _studentRepository.GetAllAsync();
            AllStudents = new ObservableCollection<Student>(students);

            var equipment = await _equipmentRepository.GetAllAsync();
            AllEquipment = new ObservableCollection<Equipment>(equipment);

            await SearchAsync();
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Failed to initialize history view: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // UC-09: Search, Filter, Sort
    [RelayCommand]
    public async Task SearchAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        IsError = false;
        try
        {
            var results = await _historyService.SearchFilterSortAsync(
                SearchText,
                SelectedStatusFilter,
                SelectedSortField,
                IsSortDescending);

            var list = results.ToList();
            GlobalResults = new ObservableCollection<Borrowing>(list);
            IsEmpty = list.Count == 0;
            if (IsEmpty)
            {
                StatusMessage = "No borrowing records match the specified search/filter criteria.";
            }
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Search query failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ClearCriteriaAsync()
    {
        SearchText = string.Empty;
        SelectedStatusFilter = BorrowingStatusFilter.All;
        SelectedSortField = BorrowingSortField.CheckoutDate;
        IsSortDescending = true;
        await SearchAsync();
    }

    // UC-07: Student History
    partial void OnSelectedStudentChanged(Student? value)
    {
        if (value is not null)
        {
            _ = LoadStudentHistoryAsync(value.Id);
        }
        else
        {
            StudentHistory.Clear();
        }
    }

    public async Task LoadStudentHistoryAsync(int studentId)
    {
        IsLoading = true;
        StatusMessage = null;
        IsError = false;
        try
        {
            var result = await _historyService.GetStudentHistoryAsync(studentId);
            if (result.IsSuccess)
            {
                var list = result.Borrowings.ToList();
                StudentHistory = new ObservableCollection<Borrowing>(list);
                if (list.Count == 0)
                {
                    StatusMessage = $"Student '{result.Student!.Name}' has no borrowing history.";
                }
            }
            else
            {
                IsError = true;
                StatusMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Failed to load student history: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // UC-08: Equipment History
    partial void OnSelectedEquipmentChanged(Equipment? value)
    {
        if (value is not null)
        {
            _ = LoadEquipmentHistoryAsync(value.Id);
        }
        else
        {
            EquipmentHistory.Clear();
        }
    }

    public async Task LoadEquipmentHistoryAsync(int equipmentId)
    {
        IsLoading = true;
        StatusMessage = null;
        IsError = false;
        try
        {
            var result = await _historyService.GetEquipmentHistoryAsync(equipmentId);
            if (result.IsSuccess)
            {
                var list = result.Borrowings.ToList();
                EquipmentHistory = new ObservableCollection<Borrowing>(list);
                if (list.Count == 0)
                {
                    StatusMessage = $"Equipment '{result.Equipment!.Name}' has no borrowing history.";
                }
            }
            else
            {
                IsError = true;
                StatusMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Failed to load equipment history: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
