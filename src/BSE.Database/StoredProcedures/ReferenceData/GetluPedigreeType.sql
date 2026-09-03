
CREATE PROCEDURE GetluPedigreeType AS

DECLARE @ttblPedigreeType TABLE
		(
		[ID] int IDENTITY(1, 1),
		[Code] char(2),
		[Description] varchar(50)
		)

	SET NOCOUNT ON

	INSERT INTO @ttblPedigreeType
		(
		[Code],
		[Description]
		)
	SELECT
		[Code],
		[Description]
	FROM
		luPedigreeType
	ORDER BY
		[Description]

	SELECT
		[ID],
		[Code],
		[Description]
	FROM
		@ttblPedigreeType

	SET NOCOUNT OFF

RETURN
