
CREATE PROCEDURE GetluAnimalOrigin AS

DECLARE @ttblAnimalOrigin TABLE
		(
		[ID] int IDENTITY(1, 1),
		[Code] char(1),
		[Description] varchar(10)
		)

	SET NOCOUNT ON

	INSERT INTO @ttblAnimalOrigin
		(
		[Code],
		[Description]
		)
	SELECT
		[Code],
		[Description]
	FROM
		luAnimalOrigin
	ORDER BY
		[Description]

	SELECT
		[ID],
		[Code],
		[Description]
	FROM
		@ttblAnimalOrigin

	SET NOCOUNT OFF

RETURN
