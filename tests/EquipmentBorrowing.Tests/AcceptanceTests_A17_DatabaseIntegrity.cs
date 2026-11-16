using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;

namespace EquipmentBorrowing.Tests;

public class AcceptanceTests_A17_DatabaseIntegrity
{
    private EquipmentBorrowingDbContext CreatePostgreSqlContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<EquipmentBorrowingDbContext>();
        optionsBuilder.UseNpgsql(DatabaseConfiguration.GetConnectionString());
        return new EquipmentBorrowingDbContext(optionsBuilder.Options);
    }

    [Fact]
    public async Task A17_PostgreSql_EnforcesUniqueStudentNumber()
    {
        using var context = CreatePostgreSqlContext();
        await context.Database.MigrateAsync();

        var randomNum = $"TEST-S-{Guid.NewGuid().ToString("N")[..6]}";

        var s1 = new Student(0, randomNum, "Test Student 1", null, true);
        context.Students.Add(s1);
        await context.SaveChangesAsync();

        // Attempt duplicate insert
        var s2 = new Student(0, randomNum, "Test Student 2", null, true);
        context.Students.Add(s2);

        var ex = await Assert.ThrowsAnyAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.NotNull(ex);
    }

    [Fact]
    public async Task A17_PostgreSql_EnforcesUniqueEquipmentIdentifier()
    {
        using var context = CreatePostgreSqlContext();
        await context.Database.MigrateAsync();

        var randomId = $"TEST-EQ-{Guid.NewGuid().ToString("N")[..6]}";

        var e1 = new Equipment(0, randomId, "Test Equipment 1", null, true);
        context.Equipment.Add(e1);
        await context.SaveChangesAsync();

        // Attempt duplicate insert
        var e2 = new Equipment(0, randomId, "Test Equipment 2", null, true);
        context.Equipment.Add(e2);

        var ex = await Assert.ThrowsAnyAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.NotNull(ex);
    }

    [Fact]
    public async Task A17_PostgreSql_EnforcesSingleActiveBorrowingPerEquipment()
    {
        using var context = CreatePostgreSqlContext();
        await context.Database.MigrateAsync();

        // Create student and equipment
        var student = new Student(0, $"S-{Guid.NewGuid().ToString("N")[..6]}", "Active Borrower", null, true);
        var equipment = new Equipment(0, $"EQ-{Guid.NewGuid().ToString("N")[..6]}", "Exclusive Tool", null, false);
        context.Students.Add(student);
        context.Equipment.Add(equipment);
        await context.SaveChangesAsync();

        // 1st active borrowing
        var b1 = new Borrowing(0, student.Id, equipment.Id, DateTime.Now, DateTime.Now.AddDays(7));
        context.Borrowings.Add(b1);
        await context.SaveChangesAsync();

        // 2nd active borrowing for the exact same equipment -> Must fail in PostgreSQL due to IX_Borrowings_EquipmentId_Active!
        var b2 = new Borrowing(0, student.Id, equipment.Id, DateTime.Now, DateTime.Now.AddDays(7));
        context.Borrowings.Add(b2);

        var ex = await Assert.ThrowsAnyAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.NotNull(ex);
    }

    [Fact]
    public async Task A17_PostgreSql_EnforcesForeignKeyIntegrity()
    {
        using var context = CreatePostgreSqlContext();
        await context.Database.MigrateAsync();

        // Non-existent student and equipment IDs
        var invalidBorrowing = new Borrowing(0, 999999, 999999, DateTime.Now, DateTime.Now.AddDays(7));
        context.Borrowings.Add(invalidBorrowing);

        var ex = await Assert.ThrowsAnyAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.NotNull(ex);
    }
}
