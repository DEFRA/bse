
CREATE PROCEDURE GetluBreed AS

DECLARE @ttblBreed TABLE
		(
		[ID] int IDENTITY(1, 1),
		[Code] varchar(20),
		[FullName] varchar(50),
		[AmalgamatedName] varchar(50)
		)

	SET NOCOUNT ON

	INSERT INTO @ttblBreed
		(
		[Code],
		[FullName],
		[AmalgamatedName]
		)
	SELECT
		[Code],
		[FullName],
		[AmalgamatedName]
	FROM
		luBreed
	ORDER BY
		[FullName]

	SELECT
		[ID],
		[Code],
		[FullName],
		[AmalgamatedName]
	FROM
		@ttblBreed

	SET NOCOUNT OFF

RETURN
