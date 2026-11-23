using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentBorrowing.Api.Endpoints;

public static class StudentEndpoints
{
    public static void MapStudentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/students").WithTags("Students");

        // API-02: GET /api/v1/students
        group.MapGet("", async (
            [FromQuery] string? search,
            [FromServices] IStudentRepository studentRepo) =>
        {
            var students = await studentRepo.GetAllAsync();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim();
                students = students.Where(s =>
                    s.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    s.StudentNumber.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var result = students.Select(s => new StudentResponse(
                s.Id, s.StudentNumber, s.Name, s.ContactInfo, s.IsAllowedToBorrow
            )).ToList();

            return Results.Ok(result);
        });

        // API-03: GET /api/v1/students/{id}
        group.MapGet("/{id:int}", async (
            int id,
            [FromServices] IStudentRepository studentRepo) =>
        {
            var student = await studentRepo.GetByIdAsync(id);
            if (student == null)
            {
                return Results.NotFound(new ErrorResponse("NOT_FOUND", $"Student with ID {id} was not found."));
            }

            return Results.Ok(new StudentResponse(
                student.Id, student.StudentNumber, student.Name, student.ContactInfo, student.IsAllowedToBorrow
            ));
        });

        // API-04: POST /api/v1/students
        group.MapPost("", async (
            [FromBody] StudentCreateRequest request,
            [FromServices] IStudentRepository studentRepo,
            [FromServices] IUnitOfWork uow) =>
        {
            if (string.IsNullOrWhiteSpace(request.StudentNumber) || string.IsNullOrWhiteSpace(request.FullName))
            {
                return Results.BadRequest(new ErrorResponse("BAD_REQUEST", "studentNumber and fullName are required."));
            }

            var cleanNumber = request.StudentNumber.Trim();
            var cleanName = request.FullName.Trim();

            var existing = await studentRepo.GetByStudentNumberAsync(cleanNumber);
            if (existing != null)
            {
                return Results.Conflict(new ErrorResponse("CONFLICT", $"Student number '{cleanNumber}' already exists."));
            }

            var student = new Student(0, cleanNumber, cleanName, request.Contact, isAllowedToBorrow: true);
            await studentRepo.AddAsync(student);
            await uow.SaveChangesAsync();

            var response = new StudentResponse(
                student.Id, student.StudentNumber, student.Name, student.ContactInfo, student.IsAllowedToBorrow
            );

            return Results.Created($"/api/v1/students/{student.Id}", response);
        });

        // API-05: PUT /api/v1/students/{id}
        group.MapPut("/{id:int}", async (
            int id,
            [FromBody] StudentUpdateRequest request,
            [FromServices] IStudentRepository studentRepo,
            [FromServices] IUnitOfWork uow) =>
        {
            if (string.IsNullOrWhiteSpace(request.StudentNumber) || string.IsNullOrWhiteSpace(request.FullName))
            {
                return Results.BadRequest(new ErrorResponse("BAD_REQUEST", "studentNumber and fullName are required."));
            }

            var student = await studentRepo.GetByIdAsync(id);
            if (student == null)
            {
                return Results.NotFound(new ErrorResponse("NOT_FOUND", $"Student with ID {id} was not found."));
            }

            var cleanNumber = request.StudentNumber.Trim();
            var cleanName = request.FullName.Trim();

            var conflict = await studentRepo.GetByStudentNumberAsync(cleanNumber);
            if (conflict != null && conflict.Id != id)
            {
                return Results.Conflict(new ErrorResponse("CONFLICT", $"Student number '{cleanNumber}' is already in use by another student."));
            }

            student.UpdateDetails(cleanNumber, cleanName, request.Contact);
            await studentRepo.UpdateAsync(student);
            await uow.SaveChangesAsync();

            return Results.Ok(new StudentResponse(
                student.Id, student.StudentNumber, student.Name, student.ContactInfo, student.IsAllowedToBorrow
            ));
        });

        // API-06: PATCH /api/v1/students/{id}/eligibility
        group.MapPatch("/{id:int}/eligibility", async (
            int id,
            [FromBody] StudentEligibilityRequest request,
            [FromServices] IStudentRepository studentRepo,
            [FromServices] IUnitOfWork uow) =>
        {
            if (request.IsEligible == null)
            {
                return Results.BadRequest(new ErrorResponse("BAD_REQUEST", "isEligible boolean is required."));
            }

            var student = await studentRepo.GetByIdAsync(id);
            if (student == null)
            {
                return Results.NotFound(new ErrorResponse("NOT_FOUND", $"Student with ID {id} was not found."));
            }

            student.SetEligibility(request.IsEligible.Value);
            await studentRepo.UpdateAsync(student);
            await uow.SaveChangesAsync();

            return Results.Ok(new StudentResponse(
                student.Id, student.StudentNumber, student.Name, student.ContactInfo, student.IsAllowedToBorrow
            ));
        });

        // API-15: GET /api/v1/students/{id}/borrowings
        group.MapGet("/{id:int}/borrowings", async (
            int id,
            [FromServices] IStudentRepository studentRepo,
            [FromServices] IBorrowingRepository borrowingRepo) =>
        {
            var student = await studentRepo.GetByIdAsync(id);
            if (student == null)
            {
                return Results.NotFound(new ErrorResponse("NOT_FOUND", $"Student with ID {id} was not found."));
            }

            var history = await borrowingRepo.GetByStudentIdAsync(id);
            var result = history.Select(b => new BorrowingResponse(
                b.Id,
                b.StudentId,
                b.EquipmentId,
                b.DateBorrowed.ToString("yyyy-MM-dd"),
                b.ExpectedReturnDate.ToString("yyyy-MM-dd"),
                b.ActualReturnDate?.ToString("yyyy-MM-dd"),
                b.Status.ToString(),
                student.StudentNumber,
                student.Name,
                b.Equipment?.EquipmentIdentifier,
                b.Equipment?.Name
            )).ToList();

            return Results.Ok(result);
        });
    }
}
