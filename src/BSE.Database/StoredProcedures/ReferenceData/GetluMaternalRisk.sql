
CREATE PROCEDURE GetluMaternalRisk AS

DECLARE @ttblMaternalRisk TABLE
		(
		[ID] int IDENTITY(1, 1),
		[Code] varchar(10),
		[Description] varchar(50)
		)

	SET NOCOUNT ON

	INSERT INTO @ttblMaternalRisk
		(
		[Code],
		[Description]
		)
	SELECT
		[Code],
		[Description]
	FROM
		luMaternalRisk
	ORDER BY
		[Description]

	SELECT
		[ID],
		[Code],
		[Description]
	FROM
		@ttblMaternalRisk

	SET NOCOUNT OFF

RETURN
