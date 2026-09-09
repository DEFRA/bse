-- Slice 3 — User Management: Add UPN column to [User] table
-- 
-- Purpose: Adds a nullable UPN (User Principal Name) column to the User table to
--          support OIDC-based identity resolution. UPN is the Azure AD claim used to
--          identify users (e.g. john.smith@defra.gov.uk).
--
-- This script is idempotent — safe to run multiple times.
-- Run this script against the target database before enabling UPN-based lookup.
-- No existing data is affected (column is nullable; existing NTLogin values unchanged).
--
-- Rollback: DROP COLUMN [UPN] from [dbo].[User]. Column is nullable; no data loss.

IF NOT EXISTS (
    SELECT 1
    FROM   sys.columns
    WHERE  object_id = OBJECT_ID(N'[dbo].[User]')
    AND    name = N'UPN'
)
BEGIN
    ALTER TABLE [dbo].[User]
    ADD [UPN] NVARCHAR(256) NULL;
END
