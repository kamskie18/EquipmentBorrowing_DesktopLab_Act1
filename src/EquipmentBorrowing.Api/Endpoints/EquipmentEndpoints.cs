using EquipmentBorrowing.Api.Models;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentBorrowing.Api.Endpoints;

public static class EquipmentEndpoints
{
    public static void MapEquipmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/equipment").WithTags("Equipment");

        // API-01: GET /api/v1/equipment
        group.MapGet("", async (
            HttpContext context,
            [FromServices] IEquipmentRepository equipmentRepo) =>
        {
            bool? availableFilter = null;
            if (context.Request.Query.TryGetValue("available", out var availStr))
            {
                if (!bool.TryParse(availStr, out var parsed))
                {
                    return Results.BadRequest(new ErrorResponse("BAD_REQUEST", "Malformed boolean for 'available' parameter. Use 'true' or 'false'."));
                }
                availableFilter = parsed;
            }

            var items = availableFilter == true
                ? await equipmentRepo.GetAvailableAsync()
                : await equipmentRepo.GetAllAsync();

            var result = items.Select(e => new EquipmentResponse(
                e.Id, e.EquipmentIdentifier, e.Name, e.Description, e.IsAvailable
            )).ToList();

            return Results.Ok(result);
        });

        // API-07: GET /api/v1/equipment/{id}
        group.MapGet("/{id:int}", async (
            int id,
            [FromServices] IEquipmentRepository equipmentRepo) =>
        {
            var item = await equipmentRepo.GetByIdAsync(id);
            if (item == null)
            {
                return Results.NotFound(new ErrorResponse("NOT_FOUND", $"Equipment with ID {id} was not found."));
            }

            return Results.Ok(new EquipmentResponse(
                item.Id, item.EquipmentIdentifier, item.Name, item.Description, item.IsAvailable
            ));
        });

        // API-08: POST /api/v1/equipment
        group.MapPost("", async (
            [FromBody] EquipmentCreateRequest request,
            [FromServices] IEquipmentRepository equipmentRepo,
            [FromServices] IUnitOfWork uow) =>
        {
            if (string.IsNullOrWhiteSpace(request.EquipmentCode) || string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new ErrorResponse("BAD_REQUEST", "equipmentCode and name are required."));
            }

            var cleanCode = request.EquipmentCode.Trim();
            var cleanName = request.Name.Trim();

            var existing = await equipmentRepo.GetByIdentifierAsync(cleanCode);
            if (existing != null)
            {
                return Results.Conflict(new ErrorResponse("CONFLICT", $"Equipment code '{cleanCode}' already exists."));
            }

            var item = new Equipment(0, cleanCode, cleanName, request.Description, isAvailable: true);
            await equipmentRepo.AddAsync(item);
            await uow.SaveChangesAsync();

            var response = new EquipmentResponse(
                item.Id, item.EquipmentIdentifier, item.Name, item.Description, item.IsAvailable
            );

            return Results.Created($"/api/v1/equipment/{item.Id}", response);
        });

        // API-09: PUT /api/v1/equipment/{id}
        group.MapPut("/{id:int}", async (
            int id,
            [FromBody] EquipmentUpdateRequest request,
            [FromServices] IEquipmentRepository equipmentRepo,
            [FromServices] IUnitOfWork uow) =>
        {
            // Protected field check: isAvailable cannot be modified via PUT (BR-07, BR-14)
            if (request.IsAvailable.HasValue)
            {
                return Results.BadRequest(new ErrorResponse("BAD_REQUEST", "Modifying 'isAvailable' is protected and cannot be updated directly via PUT. Availability changes only through borrow and return workflows."));
            }

            if (string.IsNullOrWhiteSpace(request.EquipmentCode) || string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new ErrorResponse("BAD_REQUEST", "equipmentCode and name are required."));
            }

            var item = await equipmentRepo.GetByIdAsync(id);
            if (item == null)
            {
                return Results.NotFound(new ErrorResponse("NOT_FOUND", $"Equipment with ID {id} was not found."));
            }

            var cleanCode = request.EquipmentCode.Trim();
            var cleanName = request.Name.Trim();

            var conflict = await equipmentRepo.GetByIdentifierAsync(cleanCode);
            if (conflict != null && conflict.Id != id)
            {
                return Results.Conflict(new ErrorResponse("CONFLICT", $"Equipment code '{cleanCode}' is already in use by another item."));
            }

            item.UpdateDetails(cleanCode, cleanName, request.Description);
            await equipmentRepo.UpdateAsync(item);
            await uow.SaveChangesAsync();

            return Results.Ok(new EquipmentResponse(
                item.Id, item.EquipmentIdentifier, item.Name, item.Description, item.IsAvailable
            ));
        });

        // API-16: GET /api/v1/equipment/{id}/borrowings
        group.MapGet("/{id:int}/borrowings", async (
            int id,
            [FromServices] IEquipmentRepository equipmentRepo,
            [FromServices] IBorrowingRepository borrowingRepo) =>
        {
            var item = await equipmentRepo.GetByIdAsync(id);
            if (item == null)
            {
                return Results.NotFound(new ErrorResponse("NOT_FOUND", $"Equipment with ID {id} was not found."));
            }

            var history = await borrowingRepo.GetByEquipmentIdAsync(id);
            var result = history.Select(b => new BorrowingResponse(
                b.Id,
                b.StudentId,
                b.EquipmentId,
                b.DateBorrowed.ToString("yyyy-MM-dd"),
                b.ExpectedReturnDate.ToString("yyyy-MM-dd"),
                b.ActualReturnDate?.ToString("yyyy-MM-dd"),
                b.Status.ToString(),
                b.Student?.StudentNumber,
                b.Student?.Name,
                item.EquipmentIdentifier,
                item.Name
            )).ToList();

            return Results.Ok(result);
        });
    }
}
