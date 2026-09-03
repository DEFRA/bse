
CREATE PROCEDURE GetluDocumentType AS

DECLARE @ttblDocumentType TABLE
		(
		[ID] int IDENTITY(1, 1),
		[Code] varchar(5),
		[Description] varchar(50)
		)

	SET NOCOUNT ON

	INSERT INTO @ttblDocumentType
		(
		[Code],
		[Description]
		)
	SELECT
		[Code],
		[Description]
	FROM
		luDocumentType
	ORDER BY
		[Description]

	SELECT
		[ID],
		[Code],
		[Description]
	FROM
		@ttblDocumentType

	SET NOCOUNT OFF

RETURN
