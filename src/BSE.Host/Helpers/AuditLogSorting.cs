using BSE.Modules.AuditLog.Models;

namespace BSE.Host.Helpers;

internal static class AuditLogSorting
{
    public static IEnumerable<AuditLogEntry> Apply(IEnumerable<AuditLogEntry> entries, string sortColumn, bool descending)
    {
        Func<AuditLogEntry, object?> keySelector = sortColumn switch
        {
            "User" => e => e.UserName,
            "Table" => e => e.TableName,
            "Field" => e => e.FieldName,
            "Key" => e => e.Key,
            "Before" => e => e.BeforeValue,
            "After" => e => e.AfterValue,
            "Reason" => e => e.Reason,
            _ => e => e.DateTime,
        };

        return descending ? entries.OrderByDescending(keySelector) : entries.OrderBy(keySelector);
    }
}
