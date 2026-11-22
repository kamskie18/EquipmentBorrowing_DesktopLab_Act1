using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Api.Data;

public static class Lab5Seeder
{
    public static async Task ResetAndSeedAsync(EquipmentBorrowingDbContext context)
    {
        // Clear all tables
        context.Borrowings.RemoveRange(context.Borrowings);
        context.Equipment.RemoveRange(context.Equipment);
        context.Students.RemoveRange(context.Students);
        await context.SaveChangesAsync();

        // S1-S4
        var s1 = new Student(0, "ST-100", "Ada Cruz", "ada@campus.edu", isAllowedToBorrow: true);
        var s2 = new Student(0, "ST-200", "Ben Diaz", "ben@campus.edu", isAllowedToBorrow: false);
        var s3 = new Student(0, "ST-300", "Cara Lim", "cara@campus.edu", isAllowedToBorrow: true);
        var s4 = new Student(0, "ST-400", "Dani Ong", "dani@campus.edu", isAllowedToBorrow: true);

        context.Students.AddRange(s1, s2, s3, s4);
        await context.SaveChangesAsync();

        // E1-E7
        var e1 = new Equipment(0, "EQ-001", "Camera", "Digital DSLR Camera", isAvailable: false);
        var e2 = new Equipment(0, "EQ-002", "Laptop", "High performance student laptop", isAvailable: true);
        var e3 = new Equipment(0, "EQ-003", "Projector", "Classroom HD projector", isAvailable: true);
        var e4 = new Equipment(0, "EQ-004", "Tablet", "Graphic drawing tablet", isAvailable: false);
        var e5 = new Equipment(0, "EQ-005", "Microphone", "Studio recording microphone", isAvailable: false);
        var e6 = new Equipment(0, "EQ-006", "Tripod", "Camera tripod stand", isAvailable: true);
        var e7 = new Equipment(0, "EQ-007", "Speaker", "Portable PA speaker", isAvailable: true);

        context.Equipment.AddRange(e1, e2, e3, e4, e5, e6, e7);
        await context.SaveChangesAsync();

        // Initial Borrowings:
        // E1 active with S1 (borrowed 2026-11-01, expected 2026-11-09)
        var b1 = new Borrowing(0, s1.Id, e1.Id, new DateTime(2026, 11, 1), new DateTime(2026, 11, 9));

        // E3 returned by S1 on 2026-11-08 (borrowed 2026-11-02, expected 2026-11-12)
        var b2 = new Borrowing(0, s1.Id, e3.Id, new DateTime(2026, 11, 2), new DateTime(2026, 11, 12));
        b2.MarkAsReturned(new DateTime(2026, 11, 8));

        // E4 active with S3 (borrowed 2026-11-03, expected 2026-11-12)
        var b3 = new Borrowing(0, s3.Id, e4.Id, new DateTime(2026, 11, 3), new DateTime(2026, 11, 12));

        // E5 active with S3 (borrowed 2026-11-03, expected 2026-11-12)
        var b4 = new Borrowing(0, s3.Id, e5.Id, new DateTime(2026, 11, 3), new DateTime(2026, 11, 12));

        context.Borrowings.AddRange(b1, b2, b3, b4);
        await context.SaveChangesAsync();
    }
}
