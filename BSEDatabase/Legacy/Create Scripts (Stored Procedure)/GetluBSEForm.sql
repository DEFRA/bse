set ANSI_NULLS ON
set QUOTED_IDENTIFIER ON
go


CREATE PROCEDURE [dbo].[GetluBSEForm] AS

DECLARE @ttblBSEForm TABLE
		(
		[ID] int IDENTITY(1, 1),
		[Code] char(2),
		[Description] varchar(50)
		)

	SET NOCOUNT ON

	INSERT INTO @ttblBSEForm
		(
		[Code],
		[Description]
		)
	SELECT
		[Code],
		[Description]
	FROM
		luBSEForm
	ORDER BY
		[Description]

	SELECT
		[ID],
		[Code],
		[Description]
	FROM
		@ttblBSEForm

	SET NOCOUNT OFF

RETURN

 