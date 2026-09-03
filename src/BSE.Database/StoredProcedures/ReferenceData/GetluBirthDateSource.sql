
CREATE PROCEDURE GetluBirthDateSource AS

DECLARE @ttblBirthDateSource TABLE
		(
		[ID] int IDENTITY(1, 1),
		[Code] varchar(5),
		[Description] varchar(50)
		)

	SET NOCOUNT ON

	INSERT INTO @ttblBirthDateSource
		(
		[Code],
		[Description]
		)
	SELECT
		[Code],
		[Description]
	FROM
		luBirthDateSource
	ORDER BY
		[Description]

	SELECT
		[ID],
		[Code],
		[Description]
	FROM
		@ttblBirthDateSource

	SET NOCOUNT OFF

RETURN
