
CREATE PROCEDURE GetluValuationAge AS

DECLARE @ttblValuationAge TABLE
		(
		[ID] int IDENTITY(1, 1),
		[Code] char(1),
		[Description] varchar(50)
		)

	SET NOCOUNT ON

	INSERT INTO @ttblValuationAge
		(
		[Code],
		[Description]
		)
	SELECT
		[Code],
		[Description]
	FROM
		luValuationAge
	ORDER BY
		[Description]

	SELECT
		[ID],
		[Code],
		[Description]
	FROM
		@ttblValuationAge

	SET NOCOUNT OFF

RETURN
