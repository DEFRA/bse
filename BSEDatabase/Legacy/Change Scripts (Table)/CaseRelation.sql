/* To prevent any potential data loss issues, you should review this script in detail before running it outside the context of the database designer.*/
BEGIN TRANSACTION
SET QUOTED_IDENTIFIER ON
SET ARITHABORT ON
SET NUMERIC_ROUNDABORT OFF
SET CONCAT_NULL_YIELDS_NULL ON
SET ANSI_NULLS ON
SET ANSI_PADDING ON
SET ANSI_WARNINGS ON
COMMIT
BEGIN TRANSACTION
GO
ALTER TABLE dbo.CaseRelation
	DROP CONSTRAINT CK_CaseRelation_RelationRBSEPresent
GO
ALTER TABLE dbo.CaseRelation ADD
	EartagCountry varchar(2) NULL
GO
ALTER TABLE dbo.CaseRelation WITH NOCHECK ADD CONSTRAINT
	CK_CaseRelation_RelationRBSEPresent CHECK (([RelationRBSE] is null or [RelationRBSE] is not null and [Sex] is null and [BirthDay] is null and [BirthMonth] is null and [BirthYear] is null and [RelationFate] is null and [LeftDate] is null and [EartagHerdmark] is null and [Eartag] is null and [EartagCountry] is null and [Sire] is null))
GO
COMMIT 