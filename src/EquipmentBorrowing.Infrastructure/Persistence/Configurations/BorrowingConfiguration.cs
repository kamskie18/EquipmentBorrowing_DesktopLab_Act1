using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Persistence.Configurations;

public class BorrowingConfiguration : IEntityTypeConfiguration<Borrowing>
{
    public void Configure(EntityTypeBuilder<Borrowing> builder)
    {
        builder.ToTable("Borrowings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .HasColumnName("BorrowingId");

        builder.Property(b => b.DateBorrowed)
            .IsRequired()
            .HasColumnType("timestamp without time zone");

        builder.Property(b => b.ExpectedReturnDate)
            .IsRequired()
            .HasColumnType("timestamp without time zone");

        builder.Property(b => b.ActualReturnDate)
            .HasColumnType("timestamp without time zone");

        builder.Property(b => b.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.HasOne(b => b.Student)
            .WithMany(s => s.Borrowings)
            .HasForeignKey(b => b.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Equipment)
            .WithMany(e => e.Borrowings)
            .HasForeignKey(b => b.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => b.StudentId);
        builder.HasIndex(b => b.EquipmentId);

        // BR-10 / BR-16: Database-enforced single active borrowing constraint per equipment item
        builder.HasIndex(b => b.EquipmentId)
            .IsUnique()
            .HasFilter("\"Status\" = 0")
            .HasDatabaseName("IX_Borrowings_EquipmentId_Active");
    }
}