-- ============================================================================-- ============================================================================
-- DEPLOYMENT MANIFEST — One-Time Database Migration
-- ============================================================================
-- This script is idempotent and safe to re-run via CI/CD pipeline.
-- All scripts herein use DROP+CREATE or IF EXISTS guards.
--
-- Deployment checklist:
--   1. Run against target environment (DEV / STAGING / PROD)
--   2. Verify no errors in deployment log
--   4. Tag the commit after successful run (optional but recommended)
--
-- ============================================================================


-- Step 1: Load stored procedures
PRINT '--- Deploying stored procedures ---';
:r ../StoredProcedures/dbo.GetUserByEmail.sql
GO

-- Step 2: Deployment script to used to alter/create tables, columns etc
PRINT '--- Create or Alter Table, Column  ---';
:r ../Migrations/V20260914_01_User_Email_NotNull_NTLogin_Nullable.sql
GO

PRINT '=== Database deployment completed ===';