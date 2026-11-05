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
        -- Fail loudly: duplicates must be reviewed and corrected by a person before the index can be created.
        THROW 50001, 'Duplicate DBSE values exist in dbo.[Case]; resolve them before IX_Case_DBSE can be made unique.', 1;
    END

    DROP INDEX [IX_Case_DBSE] ON dbo.[Case];

    CREATE UNIQUE NONCLUSTERED INDEX [IX_Case_DBSE]
        ON dbo.[Case]([DBSE] ASC) WHERE [DBSE] IS NOT NULL WITH (FILLFACTOR = 90);
END
GO
