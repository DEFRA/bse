using System.ComponentModel.DataAnnotations;
using BSE.Modules.Search.Models;
using BSE.SharedKernel;

namespace BSE.Host.Models.ViewModels;

public class FarmSearchViewModel : SearchViewModelBase<FarmSearchResult>
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

    protected override int PageSize => 10;

    protected override IEnumerable<FarmSearchResult> ApplySorting(IReadOnlyList<FarmSearchResult> source) =>
        (SortColumn?.ToLowerInvariant(), SortDesc) switch
        {
            ("ownername",             false) => source.OrderBy(r => r.OwnerName),
            ("ownername",             true)  => source.OrderByDescending(r => r.OwnerName),
            ("address",               false) => source.OrderBy(r => r.Address),
            ("address",               true)  => source.OrderByDescending(r => r.Address),
            ("correspondenceaddress", false) => source.OrderBy(r => r.CorrespondenceAddress),
            ("correspondenceaddress", true)  => source.OrderByDescending(r => r.CorrespondenceAddress),
            ("county",                false) => source.OrderBy(r => r.County),
            ("county",                true)  => source.OrderByDescending(r => r.County),
            ("herdmark",              false) => source.OrderBy(r => r.Herdmark),
            ("herdmark",              true)  => source.OrderByDescending(r => r.Herdmark),
            ("numericherdmark",       false) => source.OrderBy(r => r.NumericHerdmark),
            ("numericherdmark",       true)  => source.OrderByDescending(r => r.NumericHerdmark),
            ("mapreference",          false) => source.OrderBy(r => r.MapReference),
            ("mapreference",          true)  => source.OrderByDescending(r => r.MapReference),
            ("aho",                   false) => source.OrderBy(r => r.Aho),
            ("aho",                   true)  => source.OrderByDescending(r => r.Aho),
            ("herdtype",              false) => source.OrderBy(r => r.HerdType),
            ("herdtype",              true)  => source.OrderByDescending(r => r.HerdType),
            ("casescount",            false) => source.OrderBy(r => r.CasesCount),
            ("casescount",            true)  => source.OrderByDescending(r => r.CasesCount),
            ("confirmedcasescount",   false) => source.OrderBy(r => r.ConfirmedCasesCount),
            ("confirmedcasescount",   true)  => source.OrderByDescending(r => r.ConfirmedCasesCount),
            _                                => source.OrderBy(r => r.Cphh),
        };

    public FarmSearchQuery ToQuery() => new(
        Cphh: CphhNormalizer.Normalize(Cphh),
        OwnerName: OwnerName ?? "",
        Address: Address ?? "",
        County: County ?? "",
        Herdmark: Herdmark ?? "",
        NumericHerdmark: NumericHerdmark ?? "",
        IsDealer: IsDealer,
        Aho: Aho ?? "",
        IncludeNonGbFarms: IncludeNonGb);
}
