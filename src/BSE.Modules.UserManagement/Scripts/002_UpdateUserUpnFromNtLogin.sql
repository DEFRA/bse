-- Slice 3 — User Management: Back-populate UPN from NTLogin
--
-- Purpose: Best-effort population of the new UPN column for existing users.
--          Sets UPN = NTLogin + '@placeholder.domain' for all users where UPN is NULL.
--
-- !! IMPORTANT — REPLACE PLACEHOLDER BEFORE EXECUTION !!
--    Replace '@placeholder.domain' with the actual Azure AD tenant domain suffix
--    (e.g. '@defra.gov.uk') before running this script in any environment.
--    Running with the placeholder value will populate incorrect UPN values.
--
-- This script is best-effort only. Users whose NTLogin does not match their real
-- Azure AD UPN local part will not be correctly matched after this update.
-- Full reconciliation should be performed once Azure AD tenant is provisioned.
--
-- Prerequisites: AddUserUpnColumn.sql must have been applied first.
-- Idempotent: only updates rows where UPN IS NULL.

UPDATE [dbo].[User]
SET    [UPN] = [NTLogin] + '@placeholder.domain'  -- REPLACE with real domain suffix
WHERE  [UPN] IS NULL;
