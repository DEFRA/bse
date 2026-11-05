using BSE.Modules.ReferenceData.Models;

namespace BSE.Host.Models;

/// <summary>Values and options for the shared feed form fields; <paramref name="Prefix"/> is the bound property name ("NewFeed" or "Feed").</summary>
public sealed record FeedFormViewModel(
    string Prefix,
    IDictionary<string, string> FieldErrors,
    IEnumerable<LookupItem> RationTypes,
    IEnumerable<LookupItem> Suppliers,
    short? YearFrom,
    short? YearTo,
    string? RationType,
    string? RationName,
    int? SupplierId,
    bool IsPrePurchase);
