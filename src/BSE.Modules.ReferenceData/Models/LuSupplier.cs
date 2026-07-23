namespace BSE.Modules.ReferenceData.Models;

/// <summary>Maps to luSupplier via GetluSupplier / GetPossibleSuppliers / GetSupplierByName.</summary>
public record LuSupplier
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Details { get; init; }
}
