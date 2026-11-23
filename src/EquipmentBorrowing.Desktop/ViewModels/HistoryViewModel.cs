using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Desktop.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly IEquipmentBorrowingApiClient _apiClient;

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

    public HistoryViewModel(IEquipmentBorrowingApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    partial void OnSelectedTabChanged(int value)
    {
        _ = LoadTabContentAsync(value);
    }

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

    [RelayCommand]
    public async Task LoadAsync()
    {
        await LoadTabContentAsync(SelectedTab);
    }

    [RelayCommand]
    public async Task LoadTabContentAsync(int tabIndex)
    {
        StatusMessage = null;
        IsError = false;

        if (tabIndex == 0)
        {
            await SearchAsync();
        }
        else if (tabIndex == 1)
        {
            await LoadStudentsListAsync();
        }
        else if (tabIndex == 2)
        {
            await LoadEquipmentListAsync();
        }
    }

    // ---- Tab 1: Global Search (UC-09) ----
    [RelayCommand]
    public async Task SearchAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var statusStr = SelectedStatusFilter switch
            {
                BorrowingStatusFilter.Active => "Active",
                BorrowingStatusFilter.Returned => "Returned",
                _ => "All"
            };

            var sortStr = SelectedSortField == BorrowingSortField.ExpectedReturnDate ? "expectedReturnDate" : "borrowedDate";
            var dirStr = IsSortDescending ? "desc" : "asc";

            var dtos = await _apiClient.GetBorrowingsAsync(SearchText, statusStr, sortStr, dirStr);
            var domainList = dtos.Select(MapToDomain);
            GlobalResults = new ObservableCollection<Borrowing>(domainList);

            if (GlobalResults.Count == 0)
            {
                StatusMessage = "No borrowing records matched the search criteria.";
            }
        }
        catch (ApiException ex)
        {
            IsError = true;
            StatusMessage = ex.IsConnectionFailure
                ? "Connection Error: API server is unreachable."
                : ex.Message;
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Search failed: {ex.Message}";
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

    // ---- Tab 2: Student History (UC-07) ----
    private async Task LoadStudentsListAsync()
    {
        try
        {
            var dtos = await _apiClient.GetStudentsAsync();
            var list = dtos.Select(s => new Student(s.Id, s.StudentNumber, s.FullName, s.Contact, s.IsEligible));
            AllStudents = new ObservableCollection<Student>(list);
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Failed to load students: {ex.Message}";
        }
    }

    private async Task LoadStudentHistoryAsync(int studentId)
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var dtos = await _apiClient.GetStudentBorrowingsAsync(studentId);
            var list = dtos.Select(MapToDomain);
            StudentHistory = new ObservableCollection<Borrowing>(list);

            if (StudentHistory.Count == 0)
            {
                StatusMessage = "This student has no borrowing history.";
            }
        }
        catch (ApiException ex)
        {
            IsError = true;
            StatusMessage = ex.IsConnectionFailure
                ? "Connection Error: API server is unreachable."
                : ex.Message;
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

    // ---- Tab 3: Equipment History (UC-08) ----
    private async Task LoadEquipmentListAsync()
    {
        try
        {
            var dtos = await _apiClient.GetEquipmentAsync();
            var list = dtos.Select(e => new Equipment(e.Id, e.EquipmentCode, e.Name, e.Description, e.IsAvailable));
            AllEquipment = new ObservableCollection<Equipment>(list);
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Failed to load equipment list: {ex.Message}";
        }
    }

    private async Task LoadEquipmentHistoryAsync(int equipmentId)
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var dtos = await _apiClient.GetEquipmentBorrowingsAsync(equipmentId);
            var list = dtos.Select(MapToDomain);
            EquipmentHistory = new ObservableCollection<Borrowing>(list);

            if (EquipmentHistory.Count == 0)
            {
                StatusMessage = "This equipment item has no borrowing history.";
            }
        }
        catch (ApiException ex)
        {
            IsError = true;
            StatusMessage = ex.IsConnectionFailure
                ? "Connection Error: API server is unreachable."
                : ex.Message;
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

    private static Borrowing MapToDomain(BorrowingResponse d)
    {
        var b = new Borrowing(
            d.Id,
            d.StudentId,
            d.EquipmentId,
            DateTime.Parse(d.BorrowedDate),
            DateTime.Parse(d.ExpectedReturnDate)
        );

        if (!string.IsNullOrEmpty(d.ReturnedDate))
        {
            b.MarkAsReturned(DateTime.Parse(d.ReturnedDate));
        }

        if (!string.IsNullOrEmpty(d.StudentName))
        {
            typeof(Borrowing).GetProperty("Student")?.SetValue(b, new Student(d.StudentId, d.StudentNumber ?? "", d.StudentName, null, true));
        }

        if (!string.IsNullOrEmpty(d.EquipmentName))
        {
            typeof(Borrowing).GetProperty("Equipment")?.SetValue(b, new Equipment(d.EquipmentId, d.EquipmentCode ?? "", d.EquipmentName, null, false));
        }

        return b;
    }
}
