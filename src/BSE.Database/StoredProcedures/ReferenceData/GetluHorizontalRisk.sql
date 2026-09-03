
CREATE PROCEDURE GetluHorizontalRisk AS

DECLARE @ttblHorizontalRisk TABLE
		(
		[ID] int IDENTITY(1, 1),
		[Code] varchar(10),
		[Description] varchar(50)
		)

	SET NOCOUNT ON

	INSERT INTO @ttblHorizontalRisk
		(
		[Code],
		[Description]
		)
	SELECT
		[Code],
		[Description]
	FROM
		luHorizontalRisk
	ORDER BY
		[Description]

	SELECT
		[ID],
		[Code],
		[Description]
	FROM
		@ttblHorizontalRisk

	SET NOCOUNT OFF

RETURN
