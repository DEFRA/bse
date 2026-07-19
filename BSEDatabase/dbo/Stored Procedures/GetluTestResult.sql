
CREATE PROCEDURE GetluTestResult AS

DECLARE @ttblTestResult TABLE
		(
		[ID] int IDENTITY(1, 1),
		[Code] varchar(5),
		[Description] varchar(50)
		)

	SET NOCOUNT ON

	INSERT INTO @ttblTestResult
		(
		[Code],
		[Description]
		)
	SELECT
		[Code],
		[Description]
	FROM
		luTestResult
	ORDER BY
		[Description]

	SELECT
		[ID],
		[Code],
		[Description]
	FROM
		@ttblTestResult

	SET NOCOUNT OFF

RETURN
