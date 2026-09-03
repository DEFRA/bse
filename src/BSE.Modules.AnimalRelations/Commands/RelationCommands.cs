namespace BSE.Modules.AnimalRelations.Commands;

/// <summary>Maps to the <c>AddCaseRelation</c> stored procedure (13 parameters).</summary>
public sealed record AddCaseRelationCommand(
    string Rbse,
    string RelationType,
    string? RelationRbse,
    string? Sex,
    byte? BirthDay,
    byte? BirthMonth,
    short? BirthYear,
    string? RelationFate,
    DateTime? LeftDate,
    string? EartagCountry,
    string? EartagHerdmark,
    string? Eartag,
    string? Sire);

/// <summary>Maps to the <c>EditCaseRelation</c> stored procedure (13 parameters + RowStamp).</summary>
public sealed record EditCaseRelationCommand(
    int Id,
    string RelationType,
    string? RelationRbse,
    string? Sex,
    byte? BirthDay,
    byte? BirthMonth,
    short? BirthYear,
    string? RelationFate,
    DateTime? LeftDate,
    string? EartagCountry,
    string? EartagHerdmark,
    string? Eartag,
    string? Sire,
    byte[] RowStamp);

/// <summary>Maps to the <c>DeleteCaseRelation</c> stored procedure.</summary>
public sealed record DeleteCaseRelationCommand(int Id, byte[] RowStamp);
