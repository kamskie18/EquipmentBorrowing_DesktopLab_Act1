using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Infrastructure.Repositories;

var studentRepository = new InMemoryStudentRepository();
var equipmentRepository = new InMemoryEquipmentRepository();
var borrowingRepository = new InMemoryBorrowingRepository();

var borrowService = new BorrowEquipmentService(studentRepository, equipmentRepository, borrowingRepository);

Console.WriteLine("=== Successful Case: Student 1 borrows Equipment 1 ===");
var result1 = await borrowService.BorrowAsync(studentId: 1, equipmentId: 1);
Console.WriteLine(result1.IsSuccess
    ? $"SUCCESS: Borrowing #{result1.Borrowing!.Id} created for Student 1."
    : $"FAILED: {result1.ErrorMessage}");

Console.WriteLine();
Console.WriteLine("=== Failure Case: Student 2 (not allowed) tries to borrow Equipment 2 ===");
var result2 = await borrowService.BorrowAsync(studentId: 2, equipmentId: 2);
Console.WriteLine(result2.IsSuccess
    ? $"SUCCESS: Borrowing #{result2.Borrowing!.Id} created."
    : $"FAILED: {result2.ErrorMessage}");

Console.WriteLine();
Console.WriteLine("=== Failure Case: Student 1 tries to borrow already-unavailable Equipment 2 ===");
var result3 = await borrowService.BorrowAsync(studentId: 1, equipmentId: 2);
Console.WriteLine(result3.IsSuccess
    ? $"SUCCESS: Borrowing #{result3.Borrowing!.Id} created."
    : $"FAILED: {result3.ErrorMessage}");