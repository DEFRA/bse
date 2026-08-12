using System.ComponentModel.DataAnnotations;
using BSE.Modules.Search.Models;

namespace BSE.Host.Models.ViewModels;

public class FarmSearchViewModel
{
    [RegularExpression("^(?:\\d{2}(/)?\\d{3}(/)?\\d{4}(/)?\\d{2})?$", ErrorMessage = "Enter CPHH in the format NN/NNN/NNNN/NN or digits only.")]
    public string? Cphh { get; set; }
    public string? OwnerName { get; set; }
    public string? Address { get; set; }
    public string? County { get; set; }
    public string? Herdmark { get; set; }
    [RegularExpression("^(?:\\d{6})?$", ErrorMessage = "Numeric herdmark must be 6 digits.")]
    public string? NumericHerdmark { get; set; }
    public bool? IsDealer { get; set; }
    public string? Aho { get; set; }
    public bool IncludeNonGb { get; set; }

    public int PageNumber { get; set; } = 1;

    public IReadOnlyList<FarmSearchResult> Results { get; set; } = [];
    public bool HasSearched { get; set; }

    public const int PageSize = 50;
    public int TotalCount => Results.Count;
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public IReadOnlyList<FarmSearchResult> PagedResults =>
        Results.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

    public FarmSearchQuery ToQuery() => new(
        Cphh: Cphh ?? "",
        OwnerName: OwnerName ?? "",
        Address: Address ?? "",
        County: County ?? "",
        Herdmark: Herdmark ?? "",
        NumericHerdmark: NumericHerdmark ?? "",
        IsDealer: IsDealer,
        Aho: Aho ?? "",
        IncludeNonGbFarms: IncludeNonGb);
}
