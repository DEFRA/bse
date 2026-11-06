-- BSE-008: DBSE must be unique. Replaces the non-unique IX_Case_DBSE with a filtered unique index.
-- Filtered indexes require these SET options (Msg 1934).
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Case_DBSE' AND object_id = OBJECT_ID('dbo.[Case]') AND is_unique = 0
)
BEGIN
    IF EXISTS (SELECT [DBSE] FROM dbo.[Case] WHERE [DBSE] IS NOT NULL GROUP BY [DBSE] HAVING COUNT(*) > 1)
    BEGIN
        -- Legacy data already holds duplicates: warn and skip rather than fail the whole deployment.
        -- EditCaseFinalResult now serialises allocation, so no new duplicates are created.
        RAISERROR('WARNING: duplicate DBSE values exist in dbo.[Case]; IX_Case_DBSE left non-unique. Resolve the duplicates and re-run to enforce uniqueness.', 10, 1) WITH NOWAIT;
    END
    ELSE
    BEGIN
        DROP INDEX [IX_Case_DBSE] ON dbo.[Case];

        CREATE UNIQUE NONCLUSTERED INDEX [IX_Case_DBSE]
            ON dbo.[Case]([DBSE] ASC) WHERE [DBSE] IS NOT NULL WITH (FILLFACTOR = 90);
    END
END
GO
