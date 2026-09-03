
CREATE PROCEDURE GetluAnimalStatus AS

DECLARE @ttblAnimalStatus TABLE
		(
		[ID] int IDENTITY(1, 1),
		[Code] varchar(9),
		[Description] varchar(50)
		)

	SET NOCOUNT ON

	INSERT INTO @ttblAnimalStatus
		(
		[Code],
		[Description]
		)
	SELECT
		[Code],
		[Description]
	FROM
		luAnimalStatus
	ORDER BY
		[Description]

	SELECT
		[ID],
		[Code],
		[Description]
	FROM
		@ttblAnimalStatus

	SET NOCOUNT OFF

RETURN
