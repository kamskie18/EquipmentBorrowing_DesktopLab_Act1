using EquipmentBorrowing.Api.Data;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentBorrowing.Api.Endpoints;

public static class DevEndpoints
{
    public static void MapDevEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/dev").WithTags("Developer / Testing");

        // POST /api/v1/dev/seed - resets database to Section 3 reproducible dataset
        group.MapPost("/seed", async ([FromServices] EquipmentBorrowingDbContext context) =>
        {
            await Lab5Seeder.ResetAndSeedAsync(context);
            return Results.Ok(new { message = "Controlled Section 3 test dataset restored successfully (S1-S4, E1-E7)." });
        });
    }
}
