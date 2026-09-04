namespace BSE.Host.Helpers;

public static class HelpNavigation
{
    private static readonly IReadOnlyList<(string Prefix, string Fragment)> RouteMap =
    [
        ("/Home", "home-page"),

        ("/Case/Farm", "CaseEntryFarm"),
        ("/Case/DefraEdit", "CaseEntryDEFRA"),
        ("/Case/Bab", "CaseEntryBAB"),
        ("/Case/BabEdit", "CaseEntryBAB"),
        ("/Case/Vla", "CaseEntryVLA"),
        ("/Case/VlaEdit", "CaseEntryVLA"),
        ("/Case/Clinical", "CaseEntryClinical"),
        ("/Case/ClinicalEdit", "CaseEntryClinical"),
        ("/Case/Feeds", "CaseEntryFeeds"),
        ("/Case/FeedAdd", "CaseEntryFeeds"),
        ("/Case/Relations", "CaseEntryRelations"),
        ("/Case/RelationsEdit", "CaseEntryRelations"),
        ("/Case/OtherOwners", "CaseEntryRelations"),
        ("/Case/NewNonGb", "non-gb-case"),
        ("/Case/New", "case-details"),
        ("/Case/Lookup", "case-details"),
        ("/Case/Details", "case-details"),
        ("/Case/Edit", "case-details"),
        ("/Case/TestResults", "case-details"),
        ("/Case/FinalResultEntry", "final-result-entry"),
        ("/Case/MoveCase", "MoveCase"),
        ("/Case/Delete", "DeleteCase"),
        ("/Case/RbseChange", "RBSEChange"),

        ("/Farm/CphhChange", "CPHHChange"),
        ("/Farm/Lookup", "CaseEntryFarm"),
        ("/Farm/Details", "CaseEntryFarm"),
        ("/Farm/Edit", "CaseEntryFarm"),
        ("/Farm/New", "CaseEntryFarm"),

        ("/Search/Cases", "SearchCase"),
        ("/Search/CasesByHerdmark", "SearchCaseByHerdMark"),
        ("/Search/CasesByHoldingHerdmark", "SearchCPHH"),
        ("/Search/Cphh", "SearchCPHH"),
        ("/Search/Farms", "SearchFarm"),
        ("/Search/RelatedAnimals", "SearchRelatedAnimal"),
        ("/Search/Outstanding", "SearchOutstandingData"),

        ("/AuditLog/Menu", "audit-log"),
        ("/AuditLog/ByDate", "AuditLogByDate"),
        ("/AuditLog/NewFarms", "AuditLogNewFarms"),
        ("/AuditLog/CphhChanges", "AuditLogCPHHChanges"),
        ("/AuditLog/RbseChanges", "AuditLogRBSEChanges"),
        ("/AuditLog/CaseMoves", "AuditLogCaseMoves"),
        ("/AuditLog/ByUser", "AuditLogByUser"),
        ("/AuditLog/ByCase", "audit-log"),
        ("/AuditLog/ByFarm", "audit-log"),

        ("/Bsess/Menu", "bsess-check"),
        ("/Bsess/CheckByDate", "BSESSCheckByDate"),
        ("/Bsess/CheckByRbse", "BSESSCheckByRBSE"),

        ("/AdnsExport/Menu", "adns-export"),
        ("/AdnsExport/Gb", "ADNSExportGB"),
        ("/AdnsExport/Ci", "ADNSExportCI"),
        ("/AdnsExport/Ni", "ADNSExportNI"),

        ("/CaseWork/PrintBatch", "print-batch"),
        ("/Batch/PrintBatch", "print-batch"),
        ("/OssExport/Menu", "oss-export"),

        ("/Admin/PickLists", "PickListMaintenance"),
        ("/Admin/Users", "UserMaintenance"),

        ("/Error", "application-error")
    ];

    public static string? GetHelpFragment(string? requestPath)
    {
        if (string.IsNullOrWhiteSpace(requestPath))
        {
            return null;
        }

        var path = NormalizePath(requestPath);

        if (path.StartsWith("/Help", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        foreach (var (prefix, fragment) in RouteMap)
        {
            if (IsExactOrChildPath(path, prefix))
            {
                return fragment;
            }
        }

        return null;
    }

    private static string NormalizePath(string path)
    {
        var normalized = path.Trim();

        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized;
        }

        if (normalized.Length > 1 && normalized.EndsWith('/'))
        {
            normalized = normalized[..^1];
        }

        return normalized;
    }

    private static bool IsExactOrChildPath(string path, string prefix)
        => path.Equals(prefix, StringComparison.OrdinalIgnoreCase)
           || path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase);
}