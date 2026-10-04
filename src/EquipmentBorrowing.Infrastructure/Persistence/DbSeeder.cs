using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public static class DbSeeder
{
    public static void Seed(EquipmentBorrowingDbContext context)
    {
        if (context.Students.Any() || context.Equipment.Any())
            return; // already seeded — never overwrite existing data

        context.Students.AddRange(
            new Student(1, "Juan Dela Cruz", isAllowedToBorrow: true),
            new Student(2, "Maria Santos", isAllowedToBorrow: false),
            new Student(3, "Pedro Reyes", isAllowedToBorrow: true)
        );

        context.Equipment.AddRange(
            new Equipment(1, "Digital Multimeter", isAvailable: true),
            new Equipment(2, "Oscilloscope", isAvailable: true),
            new Equipment(3, "Soldering Iron", isAvailable: false)
        );

        context.SaveChanges();
    }
}