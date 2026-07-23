namespace BSE.Modules.ReferenceData.Models;

/// <summary>Maps to luAuthority via GetluAuthorityByAuthorityCounty.</summary>
public record LuAuthority
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
