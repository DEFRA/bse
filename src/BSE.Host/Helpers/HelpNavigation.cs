namespace BSE.Host.Helpers;

public static class HelpNavigation
{
    private const string CaseEntryFarmAnchor = "CaseEntryFarm";

    private const string CaseEntryRelationsAnchor = "CaseEntryRelations";

    private const string CaseEntryVlaAnchor = "CaseEntryVLA";

    private static readonly IReadOnlyList<(string Prefix, string Fragment)> RouteMap =
    [
        ("/Home", "home-page"),

        ("/Case/Farm", CaseEntryFarmAnchor),
        ("/Case/DefraEdit", "CaseEntryDEFRA"),
        ("/Case/Bab", "CaseEntryBAB"),
        ("/Case/BabEdit", "CaseEntryBAB"),
        ("/Case/Vla", CaseEntryVlaAnchor),
        ("/Case/VlaEdit", CaseEntryVlaAnchor),
        ("/Case/Clinical", "CaseEntryClinical"),
        ("/Case/ClinicalEdit", "CaseEntryClinical"),
        ("/Case/Feeds", "CaseEntryFeeds"),
        ("/Case/FeedAdd", "CaseEntryFeeds"),
        ("/Case/Relations", CaseEntryRelationsAnchor),
        ("/Case/RelationAdd", CaseEntryRelationsAnchor),
        ("/Case/RelationEdit", CaseEntryRelationsAnchor),
        ("/Case/PickSireDam", CaseEntryRelationsAnchor),
        ("/Case/RelationsView", CaseEntryRelationsAnchor),
        ("/Case/OtherOwnerAdd", CaseEntryVlaAnchor),
        ("/Case/OtherOwnerEdit", CaseEntryVlaAnchor),
        ("/Case/NewNonGb", "non-gb-case"),
        ("/Case/New", "case-details"),
        ("/Case/Edit", "case-details"),
        ("/Case/TestResults", "case-details"),
        ("/Case/FinalResultEntry", "final-result-entry"),
        ("/Case/MoveCase", "MoveCase"),
        ("/Case/Delete", "DeleteCase"),
        ("/Case/RbseChange", "RBSEChange"),

        ("/Farm/CphhChange", "CPHHChange"),
        ("/Farm/Lookup", CaseEntryFarmAnchor),
        ("/Farm/Details", CaseEntryFarmAnchor),
        ("/Farm/Edit", CaseEntryFarmAnchor),
        ("/Farm/New", CaseEntryFarmAnchor),
        ("/Farm/MoveCaseNewFarm", "MoveCase"),

        ("/Search/Cases", "SearchCase"),
        ("/Search/CasesByHerdmark", "SearchCaseByHerdMark"),
        ("/Search/CasesByHoldingHerdmark", "SearchCPHH"),
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