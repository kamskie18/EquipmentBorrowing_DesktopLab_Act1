using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
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
    private readonly IStudentRepository _studentRepository;
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly BorrowEquipmentService _borrowSingleService;
    private readonly MultiItemBorrowService _multiBorrowService;

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

    public CheckoutViewModel(
        IStudentRepository studentRepository,
        IEquipmentRepository equipmentRepository,
        BorrowEquipmentService borrowSingleService,
        MultiItemBorrowService multiBorrowService)
    {
        _studentRepository = studentRepository;
        _equipmentRepository = equipmentRepository;
        _borrowSingleService = borrowSingleService;
        _multiBorrowService = multiBorrowService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var studentList = await _studentRepository.GetAllAsync();
            Students = new ObservableCollection<Student>(studentList);

            var equipList = await _equipmentRepository.GetAvailableAsync();
            AvailableEquipment = new ObservableCollection<EquipmentSelectionItem>(
                equipList.Select(e => new EquipmentSelectionItem(e)));
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

        var result = await _borrowSingleService.BorrowAsync(
            SelectedStudent.Id,
            selectedItems[0].Equipment.Id,
            ExpectedReturnDate.DateTime);

        if (result.IsSuccess)
        {
            StatusMessage = $"Single item '{selectedItems[0].Equipment.Name}' borrowed successfully. Due: {result.Borrowing!.ExpectedReturnDate:MMM dd, yyyy}.";
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

        var result = await _multiBorrowService.CheckOutMultipleAsync(
            SelectedStudent.Id,
            selectedIds,
            ExpectedReturnDate.DateTime,
            simulateFailureAfterFirstWrite: SimulateFailureSeam);

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
            // Reload to prove no partial state changed
            await LoadAsync();
        }
    }
}
