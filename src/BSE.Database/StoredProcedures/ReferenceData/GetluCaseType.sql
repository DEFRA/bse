
CREATE PROCEDURE [dbo].[GetluCaseType] AS

DECLARE @ttblCaseType TABLE
		(
		[ID] int IDENTITY(1, 1),
		[Code] char(2),
		[Description] varchar(50)
		)

	SET NOCOUNT ON

	INSERT INTO @ttblCaseType
		(
		[Code],
		[Description]
		)
	SELECT
		[Code],
		[Description]
	FROM
		luCaseType
	ORDER BY
		[Description]

	SELECT
		[ID],
		[Code],
		[Description]
	FROM
		@ttblCaseType

	SET NOCOUNT OFF

RETURN


