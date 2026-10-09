using System.Text.Json;

namespace BSE.Host.Helpers;

/// <summary>
/// Detects whether a scalar edit command staged by a tab-switch ("StageAndGoto") actually differs
/// from what is already persisted, so merely navigating between tabs without typing anything never
/// flags the case as having unsaved changes. Byte-array fields (e.g. RowStamp) compare correctly
/// here because JSON-encodes them as base64 text rather than comparing array references.
/// </summary>
public static class DraftChangeDetector
{
    public static bool IsDifferentFromPersisted<T>(T newCommand, T? persistedEquivalent)
        => persistedEquivalent is null
           || JsonSerializer.Serialize(newCommand) != JsonSerializer.Serialize(persistedEquivalent);
}
