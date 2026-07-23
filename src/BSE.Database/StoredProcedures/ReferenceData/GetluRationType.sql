
CREATE PROCEDURE GetluRationType AS

DECLARE @ttblRationType TABLE
		(
		[ID] int IDENTITY(1, 1),
		[Code] varchar(2),
		[Description] varchar(50)
		)

	SET NOCOUNT ON

	INSERT INTO @ttblRationType
		(
		[Code],
		[Description]
		)
	SELECT
		[Code],
		[Description]
	FROM
		luRationType
	ORDER BY
		[Description]

	SELECT
		[ID],
		[Code],
		[Description]
	FROM
		@ttblRationType

	SET NOCOUNT OFF

RETURN
