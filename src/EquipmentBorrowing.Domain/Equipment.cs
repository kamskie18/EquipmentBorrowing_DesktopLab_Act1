namespace EquipmentBorrowing.Domain;

public class Equipment
{
    public int Id { get; private set; }
    public string EquipmentIdentifier { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsAvailable { get; private set; }

    public ICollection<Borrowing> Borrowings { get; private set; } = new List<Borrowing>();

    // For EF Core
    protected Equipment() { }

    public Equipment(int id, string equipmentIdentifier, string name, string? description = null, bool isAvailable = true)
    {
        Id = id;
        EquipmentIdentifier = equipmentIdentifier?.Trim() ?? string.Empty;
        Name = name?.Trim() ?? string.Empty;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsAvailable = isAvailable;
    }

    // Backwards-compatible constructor for existing tests/seeds
    public Equipment(int id, string name, bool isAvailable)
        : this(id, $"EQ-{id:D3}", name, null, isAvailable)
    {
    }

    public void UpdateDetails(string equipmentIdentifier, string name, string? description)
    {
        EquipmentIdentifier = equipmentIdentifier?.Trim() ?? string.Empty;
        Name = name?.Trim() ?? string.Empty;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void MarkAsBorrowed() => IsAvailable = false;
    public void MarkAsAvailable() => IsAvailable = true;
}