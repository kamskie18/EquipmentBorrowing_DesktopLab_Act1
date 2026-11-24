using EquipmentBorrowing.Api.Data;
using EquipmentBorrowing.Api.Endpoints;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Models;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Infrastructure.Persistence;
using EquipmentBorrowing.Infrastructure.Repositories;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configure DbContext with PostgreSQL
builder.Services.AddDbContext<EquipmentBorrowingDbContext>(options =>
{
    options.UseNpgsql(DatabaseConfiguration.GetConnectionString());
});

// Unit of Work and Repositories
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<IStudentRepository, EfStudentRepository>();
builder.Services.AddScoped<IEquipmentRepository, EfEquipmentRepository>();
builder.Services.AddScoped<IBorrowingRepository, EfBorrowingRepository>();

// Application Services
builder.Services.AddTransient<BorrowEquipmentService>();
builder.Services.AddTransient<ReturnEquipmentService>();
builder.Services.AddTransient<MultiItemBorrowService>();
builder.Services.AddTransient<StudentManagementService>();
builder.Services.AddTransient<EquipmentManagementService>();
builder.Services.AddTransient<BorrowingHistoryService>();

// OpenAPI / Swagger
builder.Services.AddOpenApi();

// CORS for local Desktop & testing tools
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Enable CORS
app.UseCors();

// Global Exception Handler: enforce { "code": "...", "message": "..." } with 0 stack traces/credentials
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new ErrorResponse("INTERNAL_ERROR", "An unexpected server error occurred. Please contact the system administrator."));
    });
});

// Status Code Pages: 404 and 405 return standard contract
app.UseStatusCodePages(async statusCodeContext =>
{
    var code = statusCodeContext.HttpContext.Response.StatusCode;
    statusCodeContext.HttpContext.Response.ContentType = "application/json";
    if (code == StatusCodes.Status404NotFound)
    {
        await statusCodeContext.HttpContext.Response.WriteAsJsonAsync(new ErrorResponse("NOT_FOUND", "The requested endpoint or resource was not found."));
    }
    else if (code == StatusCodes.Status405MethodNotAllowed)
    {
        await statusCodeContext.HttpContext.Response.WriteAsJsonAsync(new ErrorResponse("METHOD_NOT_ALLOWED", "The HTTP method is not allowed for this route."));
    }
});

// OpenAPI & Interactive Scalar Documentation
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("Campus Equipment Borrowing API (ITSD81 - Lab 5)")
           .WithTheme(ScalarTheme.BluePlanet)
           .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

// System Health Check Endpoint
app.MapGet("/api/v1/health", async (EquipmentBorrowingDbContext db) =>
{
    var canConnect = false;
    try
    {
        canConnect = await db.Database.CanConnectAsync();
    }
    catch
    {
        canConnect = false;
    }

    return Results.Ok(new
    {
        status = canConnect ? "Healthy" : "Degraded",
        database = canConnect ? "Connected" : "Disconnected",
        service = "EquipmentBorrowing.Api",
        timestamp = DateTime.UtcNow.ToString("o")
    });
}).WithTags("System");

// Map all endpoints (API-01 to API-16 + Dev)
app.MapStudentEndpoints();
app.MapEquipmentEndpoints();
app.MapBorrowingEndpoints();
app.MapDevEndpoints();

// Root redirect to Scalar docs for convenience
app.MapGet("/", () => Results.Redirect("/scalar/v1"));

// Auto-migrate & seed controlled dataset on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EquipmentBorrowingDbContext>();
    try
    {
        await context.Database.MigrateAsync();
        if (!await context.Students.AnyAsync() && !await context.Equipment.AnyAsync())
        {
            await Lab5Seeder.ResetAndSeedAsync(context);
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Could not automatically migrate or seed database on startup.");
    }
}

app.Run();
