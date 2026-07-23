using BSE.SharedKernel;

namespace BSE.Modules.ReferenceData.Models;

/// <summary>Generic two-column lookup item used for simple dropdown binding.</summary>
public record LookupItem(int Id, string Description) : ILookupItem;
