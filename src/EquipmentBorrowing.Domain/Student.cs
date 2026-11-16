namespace EquipmentBorrowing.Domain;

public class Student
{
    public int Id { get; private set; }
    public string StudentNumber { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? ContactInfo { get; private set; }
    public bool IsAllowedToBorrow { get; private set; }

    public ICollection<Borrowing> Borrowings { get; private set; } = new List<Borrowing>();

    // For EF Core
    protected Student() { }

    public Student(int id, string studentNumber, string name, string? contactInfo = null, bool isAllowedToBorrow = true)
    {
        Id = id;
        StudentNumber = studentNumber?.Trim() ?? string.Empty;
        Name = name?.Trim() ?? string.Empty;
        ContactInfo = string.IsNullOrWhiteSpace(contactInfo) ? null : contactInfo.Trim();
        IsAllowedToBorrow = isAllowedToBorrow;
    }

    // Backwards-compatible constructor for existing tests/seeds
    public Student(int id, string name, bool isAllowedToBorrow)
        : this(id, $"S{id:D4}", name, null, isAllowedToBorrow)
    {
    }

    public void UpdateDetails(string studentNumber, string name, string? contactInfo)
    {
        StudentNumber = studentNumber?.Trim() ?? string.Empty;
        Name = name?.Trim() ?? string.Empty;
        ContactInfo = string.IsNullOrWhiteSpace(contactInfo) ? null : contactInfo.Trim();
    }

    public void SetEligibility(bool isAllowed)
    {
        IsAllowedToBorrow = isAllowed;
    }
}
