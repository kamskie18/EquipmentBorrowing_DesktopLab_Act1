using EquipmentBorrowing.Api.Data;
using EquipmentBorrowing.Api.Endpoints;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Infrastructure.Persistence;
using EquipmentBorrowing.Infrastructure.Repositories;
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

// OpenAPI & Interactive Scalar Documentation
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("Campus Equipment Borrowing API (ITSD81 - Lab 5)")
           .WithTheme(ScalarTheme.BluePlanet)
           .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

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
