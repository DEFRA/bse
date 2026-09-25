-- Required for the filtered indexes created below (Msg 1934) — set explicitly rather than
-- relying on whatever a prior :r'd script (e.g. GetUserByEmail.sql sets ANSI_NULLS OFF)
-- left the session in
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

BEGIN TRANSACTION;

-- Duplicate emails can exist where the same person has two User rows: an older
-- NTLogin using the legacy single-character-prefix convention (e.g. x0391401)
-- and a newer one using the current multi-character prefix convention
-- (e.g. ns000060). For each such duplicate pair, deactivate the legacy row and
-- rename its email so it no longer collides with the active row's email,
-- before the NOT NULL/UNIQUE constraints below are enforced.
;WITH DuplicateEmails AS (
    SELECT Email
    FROM dbo.[User]
    WHERE Email IS NOT NULL
    GROUP BY Email
    HAVING COUNT(*) > 1
),
LegacyRows AS (
    SELECT u.ID, u.Email, u.NTLogin
    FROM dbo.[User] u
    INNER JOIN DuplicateEmails d ON d.Email = u.Email
    WHERE u.NTLogin LIKE '[a-zA-Z][0-9]%'
      AND u.NTLogin NOT LIKE '[a-zA-Z][a-zA-Z]%'
)
UPDATE u
SET u.IsActive = 0,
    u.Email = CONCAT(l.Email, '__LEGACY')
FROM dbo.[User] u
INNER JOIN LegacyRows l ON l.ID = u.ID;

UPDATE dbo.[User]
SET Email = CONCAT('old_email_legacy', ID, '@apha.gov.uk')
WHERE Email IS NULL;

IF EXISTS (
    SELECT 1
    FROM dbo.[User]
    WHERE Email IS NULL
)
BEGIN
    RAISERROR('NULL emails still exist.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;

ALTER TABLE dbo.[User]
ALTER COLUMN Email VARCHAR(60) NOT NULL;

-- Email is now the primary per-request user-resolution lookup (GetUserByEmail, called on
-- every authenticated request) — index it. Unique because that lookup expects one row.
IF EXISTS (
    SELECT Email FROM dbo.[User]
    GROUP BY Email
    HAVING COUNT(*) > 1
)
BEGIN
    RAISERROR('Duplicate emails exist - cannot create unique index on Email.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_User_Email' AND object_id = OBJECT_ID('dbo.[User]'))
    CREATE UNIQUE NONCLUSTERED INDEX IX_User_Email ON dbo.[User] (Email);

-- IX_User_NTLogin depends on NTLogin (Msg 5074) — must be dropped before ALTER COLUMN
-- and recreated after. Recreated as a filtered unique index when it was unique, since a
-- plain unique index tolerates only one NULL row and NTLogin can now be NULL for
-- multiple Entra ID-only users. If it backs a UNIQUE constraint (Msg 3723), DROP INDEX
-- is rejected — the constraint itself has to be dropped instead.
DECLARE @isUnique bit = NULL, @isConstraint bit = 0;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_User_NTLogin' AND object_id = OBJECT_ID('dbo.[User]'))
BEGIN
    SELECT @isUnique = is_unique FROM sys.indexes
    WHERE name = 'IX_User_NTLogin' AND object_id = OBJECT_ID('dbo.[User]');

    IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'IX_User_NTLogin' AND parent_object_id = OBJECT_ID('dbo.[User]'))
    BEGIN
        SET @isConstraint = 1;
        ALTER TABLE dbo.[User] DROP CONSTRAINT IX_User_NTLogin;
    END
    ELSE
        DROP INDEX IX_User_NTLogin ON dbo.[User];
END;

ALTER TABLE dbo.[User]
ALTER COLUMN NTLogin VARCHAR(25) NULL;

-- Recreated as a filtered unique INDEX (not a constraint) even if it was originally a
-- constraint — unique constraints can't have a WHERE filter, which is required here.
IF @isUnique = 1
    CREATE UNIQUE NONCLUSTERED INDEX IX_User_NTLogin ON dbo.[User] (NTLogin) WHERE NTLogin IS NOT NULL;
ELSE IF @isUnique = 0
    CREATE NONCLUSTERED INDEX IX_User_NTLogin ON dbo.[User] (NTLogin);

COMMIT TRANSACTION;