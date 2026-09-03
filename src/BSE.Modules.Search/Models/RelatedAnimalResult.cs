namespace BSE.Modules.Search.Models;

/// <summary>
/// Result record for GetSearchRelatedAnimals — 11-column result from the SP temp table.
/// </summary>
public record RelatedAnimalResult(
    string Rbse,
    string Cphh,
    string? RelationType,
    string? RelSex,
    string? Eartag,
    string? RelBirthDate,
    string? RelFate,
    DateTime? LeftDate,
    string? RelName,
    string? RelEartag,
    string? RelationRbse);
