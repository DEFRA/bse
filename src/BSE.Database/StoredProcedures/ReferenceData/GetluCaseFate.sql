
CREATE PROCEDURE GetluCaseFate AS

DECLARE @ttblCaseFate TABLE
		(
		[ID] int IDENTITY(1, 1),
		[Code] varchar(4),
		[Description] varchar(50)
		)

	SET NOCOUNT ON

	INSERT INTO @ttblCaseFate
		(
		[Code],
		[Description]
		)
	SELECT
		[Code],
		[Description]
	FROM
		luCaseFate
	ORDER BY
		[Description]

	SELECT
		[ID],
		[Code],
		[Description]
	FROM
		@ttblCaseFate

	SET NOCOUNT OFF

RETURN
