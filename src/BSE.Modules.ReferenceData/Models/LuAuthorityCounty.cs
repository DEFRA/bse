namespace BSE.Modules.ReferenceData.Models;

/// <summary>Maps to luAuthorityCounty via GetluAuthorityCountyAll.</summary>
public record LuAuthorityCounty
{
    public int Id { get; init; }
    public string County { get; init; } = string.Empty;
}
