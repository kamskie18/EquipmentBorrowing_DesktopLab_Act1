using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public class EquipmentBorrowingDbContextFactory : IDesignTimeDbContextFactory<EquipmentBorrowingDbContext>
{
    public EquipmentBorrowingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EquipmentBorrowingDbContext>();
        var connectionString = DatabaseConfiguration.GetConnectionString();
        optionsBuilder.UseNpgsql(connectionString);
        return new EquipmentBorrowingDbContext(optionsBuilder.Options);
    }
}