using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public static class DbSeeder
{
    public static void Seed(EquipmentBorrowingDbContext context)
    {
        if (context.Students.Any() || context.Equipment.Any())
            return; // already seeded — never overwrite existing data

        context.Students.AddRange(
            new Student(0, "S1001", "Juan Dela Cruz", "juan@campus.edu", isAllowedToBorrow: true),
            new Student(0, "S1002", "Maria Santos", "maria@campus.edu", isAllowedToBorrow: false),
            new Student(0, "S1003", "Pedro Reyes", null, isAllowedToBorrow: true),
            new Student(0, "S1004", "Ana Gonzales", "ana@campus.edu", isAllowedToBorrow: true)
        );

        context.Equipment.AddRange(
            new Equipment(0, "EQ-001", "Digital Multimeter", "Handheld digital multimeter for voltage and current testing", isAvailable: true),
            new Equipment(0, "EQ-002", "Oscilloscope", "100MHz 2-channel digital storage oscilloscope", isAvailable: true),
            new Equipment(0, "EQ-003", "Soldering Iron Station", "Temperature controlled 60W soldering station", isAvailable: true),
            new Equipment(0, "EQ-004", "DC Power Supply", "0-30V 5A variable DC bench power supply", isAvailable: true),
            new Equipment(0, "EQ-005", "Logic Analyzer", "16-channel USB logic analyzer", isAvailable: true)
        );

        context.SaveChanges();
    }
}