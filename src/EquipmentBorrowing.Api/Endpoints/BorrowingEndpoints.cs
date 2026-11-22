using System.Globalization;
using EquipmentBorrowing.Api.Models;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentBorrowing.Api.Endpoints;

public static class BorrowingEndpoints
{
    public static void MapBorrowingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1").WithTags("Borrowings");

        // API-13: GET /api/v1/borrowings/{id}
        group.MapGet("/borrowings/{id:int}", async (
            int id,
            [FromServices] IBorrowingRepository borrowingRepo) =>
        {
            var borrowing = await borrowingRepo.GetByIdAsync(id);
            if (borrowing == null)
            {
                return Results.NotFound(new ErrorResponse("NOT_FOUND", $"Borrowing record with ID {id} was not found."));
            }

            return Results.Ok(ToResponse(borrowing));
        });

        // API-14: GET /api/v1/borrowings
        group.MapGet("/borrowings", async (
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? sort,
            [FromQuery] string? direction,
            [FromServices] IBorrowingRepository borrowingRepo) =>
        {
            var statusFilter = BorrowingStatusFilter.All;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                    statusFilter = BorrowingStatusFilter.Active;
                else if (status.Equals("Returned", StringComparison.OrdinalIgnoreCase))
                    statusFilter = BorrowingStatusFilter.Returned;
                else if (status.Equals("All", StringComparison.OrdinalIgnoreCase))
                    statusFilter = BorrowingStatusFilter.All;
                else
                    return Results.BadRequest(new ErrorResponse("BAD_REQUEST", $"Unsupported status filter '{status}'. Supported values: Active, Returned, All."));
            }

            var sortField = BorrowingSortField.CheckoutDate;
            if (!string.IsNullOrWhiteSpace(sort))
            {
                if (sort.Equals("borrowedDate", StringComparison.OrdinalIgnoreCase))
                    sortField = BorrowingSortField.CheckoutDate;
                else if (sort.Equals("expectedReturnDate", StringComparison.OrdinalIgnoreCase))
                    sortField = BorrowingSortField.ExpectedReturnDate;
                else
                    return Results.BadRequest(new ErrorResponse("BAD_REQUEST", $"Unsupported sort field '{sort}'. Supported values: borrowedDate, expectedReturnDate."));
            }

            var sortDescending = false;
            if (!string.IsNullOrWhiteSpace(direction))
            {
                if (direction.Equals("asc", StringComparison.OrdinalIgnoreCase))
                    sortDescending = false;
                else if (direction.Equals("desc", StringComparison.OrdinalIgnoreCase))
                    sortDescending = true;
                else
                    return Results.BadRequest(new ErrorResponse("BAD_REQUEST", $"Unsupported direction '{direction}'. Supported values: asc, desc."));
            }

            var records = await borrowingRepo.SearchFilterSortAsync(
                search?.Trim(), statusFilter, sortField, sortDescending
            );

            var list = records.Select(ToResponse).ToList();
            return Results.Ok(list);
        });

        // API-10: POST /api/v1/borrowings
        group.MapPost("/borrowings", async (
            [FromBody] SingleBorrowRequest request,
            [FromServices] IStudentRepository studentRepo,
            [FromServices] IEquipmentRepository equipmentRepo,
            [FromServices] IBorrowingRepository borrowingRepo,
            [FromServices] IUnitOfWork uow) =>
        {
            if (!DateTime.TryParseExact(request.ExpectedReturnDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dueDate))
            {
                return Results.BadRequest(new ErrorResponse("BAD_REQUEST", "Invalid expectedReturnDate format. Must be YYYY-MM-DD."));
            }

            var checkoutDate = DateTime.Today;
            if (dueDate <= checkoutDate)
            {
                return Results.BadRequest(new ErrorResponse("BAD_REQUEST", "expectedReturnDate must be strictly later than the local checkout date."));
            }

            var student = await studentRepo.GetByIdAsync(request.StudentId);
            if (student == null)
            {
                return Results.NotFound(new ErrorResponse("NOT_FOUND", $"Student with ID {request.StudentId} was not found."));
            }

            var equipment = await equipmentRepo.GetByIdAsync(request.EquipmentId);
            if (equipment == null)
            {
                return Results.NotFound(new ErrorResponse("NOT_FOUND", $"Equipment with ID {request.EquipmentId} was not found."));
            }

            if (!student.IsAllowedToBorrow)
            {
                return Results.Conflict(new ErrorResponse("CONFLICT", $"Student '{student.Name}' is ineligible/restricted from borrowing equipment."));
            }

            if (!equipment.IsAvailable)
            {
                return Results.Conflict(new ErrorResponse("CONFLICT", $"Equipment '{equipment.Name}' is currently unavailable."));
            }

            var activeCount = await borrowingRepo.CountActiveBorrowingsAsync(student.Id);
            if (activeCount >= BorrowEquipmentService.MaxActiveBorrowings)
            {
                return Results.Conflict(new ErrorResponse("CONFLICT", $"Student has reached the maximum borrowing limit of {BorrowEquipmentService.MaxActiveBorrowings} active items."));
            }

            var borrowing = new Borrowing(0, student.Id, equipment.Id, checkoutDate, dueDate);
            equipment.MarkAsBorrowed();

            await equipmentRepo.UpdateAsync(equipment);
            await borrowingRepo.AddAsync(borrowing);
            await uow.SaveChangesAsync();

            borrowing = await borrowingRepo.GetByIdAsync(borrowing.Id) ?? borrowing;

            return Results.Created($"/api/v1/borrowings/{borrowing.Id}", ToResponse(borrowing));
        });

        // API-11: POST /api/v1/checkout-requests
        group.MapPost("/checkout-requests", async (
            HttpContext httpContext,
            [FromBody] CheckoutRequest request,
            [FromServices] IStudentRepository studentRepo,
            [FromServices] IEquipmentRepository equipmentRepo,
            [FromServices] MultiItemBorrowService multiBorrowService,
            [FromServices] IBorrowingRepository borrowingRepo) =>
        {
            if (request.EquipmentIds == null || request.EquipmentIds.Count < 2)
            {
                return Results.BadRequest(new ErrorResponse("BAD_REQUEST", "Checkout request requires at least two distinct equipment items."));
            }

            if (request.EquipmentIds.Distinct().Count() != request.EquipmentIds.Count)
            {
                return Results.BadRequest(new ErrorResponse("BAD_REQUEST", "Duplicate equipment IDs are not allowed in checkout request."));
            }

            if (!DateTime.TryParseExact(request.ExpectedReturnDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dueDate))
            {
                return Results.BadRequest(new ErrorResponse("BAD_REQUEST", "Invalid expectedReturnDate format. Must be YYYY-MM-DD."));
            }

            var checkoutDate = DateTime.Today;
            if (dueDate <= checkoutDate)
            {
                return Results.BadRequest(new ErrorResponse("BAD_REQUEST", "expectedReturnDate must be strictly later than the local checkout date."));
            }

            var student = await studentRepo.GetByIdAsync(request.StudentId);
            if (student == null)
            {
                return Results.NotFound(new ErrorResponse("NOT_FOUND", $"Student with ID {request.StudentId} was not found."));
            }

            // Verify each item exists
            foreach (var eqId in request.EquipmentIds)
            {
                var eq = await equipmentRepo.GetByIdAsync(eqId);
                if (eq == null)
                {
                    return Results.NotFound(new ErrorResponse("NOT_FOUND", $"Equipment with ID {eqId} was not found."));
                }
            }

            // Check simulate failure query for T05 acceptance fault injection
            var simulateFailure = httpContext.Request.Query.ContainsKey("simulateFailure") &&
                                 bool.TryParse(httpContext.Request.Query["simulateFailure"], out var sf) && sf;

            var result = await multiBorrowService.CheckOutMultipleAsync(
                request.StudentId,
                request.EquipmentIds,
                dueDate,
                simulateFailureAfterFirstWrite: simulateFailure
            );

            if (!result.IsSuccess)
            {
                if (simulateFailure)
                {
                    return Results.Problem(
                        statusCode: 500,
                        title: "Injected Server Failure",
                        detail: "A controlled server failure occurred during multi-item transaction and all changes were rolled back cleanly."
                    );
                }

                return Results.Conflict(new ErrorResponse("CONFLICT", result.ErrorMessage ?? "Multi-item checkout failed."));
            }

            // Refresh objects with student/equipment relationships
            var loadedList = new List<BorrowingResponse>();
            foreach (var b in result.Borrowings)
            {
                var full = await borrowingRepo.GetByIdAsync(b.Id) ?? b;
                loadedList.Add(ToResponse(full));
            }

            return Results.Created("/api/v1/borrowings", new CheckoutResponse(loadedList));
        });

        // API-12: POST /api/v1/borrowings/{id}/return
        group.MapPost("/borrowings/{id:int}/return", async (
            int id,
            [FromServices] IBorrowingRepository borrowingRepo,
            [FromServices] ReturnEquipmentService returnService) =>
        {
            var borrowing = await borrowingRepo.GetByIdAsync(id);
            if (borrowing == null)
            {
                return Results.NotFound(new ErrorResponse("NOT_FOUND", $"Borrowing record with ID {id} was not found."));
            }

            if (borrowing.Status == BorrowingStatus.Returned)
            {
                return Results.Conflict(new ErrorResponse("CONFLICT", "This borrowing record has already been returned."));
            }

            var result = await returnService.ReturnAsync(id, DateTime.Today);
            if (!result.IsSuccess)
            {
                return Results.Conflict(new ErrorResponse("CONFLICT", result.ErrorMessage ?? "Failed to return borrowing."));
            }

            var updated = await borrowingRepo.GetByIdAsync(id) ?? borrowing;
            return Results.Ok(ToResponse(updated));
        });
    }

    private static BorrowingResponse ToResponse(Borrowing b)
    {
        return new BorrowingResponse(
            b.Id,
            b.StudentId,
            b.EquipmentId,
            b.DateBorrowed.ToString("yyyy-MM-dd"),
            b.ExpectedReturnDate.ToString("yyyy-MM-dd"),
            b.ActualReturnDate?.ToString("yyyy-MM-dd"),
            b.Status.ToString(),
            b.Student?.StudentNumber,
            b.Student?.Name,
            b.Equipment?.EquipmentIdentifier,
            b.Equipment?.Name
        );
    }
}
