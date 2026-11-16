namespace EquipmentBorrowing.Domain;

public class Borrowing
{
    public int Id { get; private set; }
    public int StudentId { get; private set; }
    public Student? Student { get; private set; }

    public int EquipmentId { get; private set; }
    public Equipment? Equipment { get; private set; }

    public DateTime DateBorrowed { get; private set; }
    public DateTime ExpectedReturnDate { get; private set; }
    public DateTime? ActualReturnDate { get; private set; }
    public BorrowingStatus Status { get; private set; }

    // For EF Core
    protected Borrowing() { }

    public Borrowing(int id, int studentId, int equipmentId, DateTime dateBorrowed, DateTime expectedReturnDate)
    {
        Id = id;
        StudentId = studentId;
        EquipmentId = equipmentId;
        DateBorrowed = dateBorrowed;
        ExpectedReturnDate = expectedReturnDate;
        Status = BorrowingStatus.Active;
        ActualReturnDate = null;
    }

    public void MarkAsReturned(DateTime actualReturnDate)
    {
        Status = BorrowingStatus.Returned;
        ActualReturnDate = actualReturnDate;
    }

    public void MarkAsReturned() => MarkAsReturned(DateTime.Now);
}