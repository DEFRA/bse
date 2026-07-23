
CREATE PROCEDURE GetluOwnerType AS

DECLARE @ttblOwnerType TABLE
		(
		[ID] int IDENTITY(1, 1),
		[Code] varchar(10),
		[Description] varchar(50)
		)

	SET NOCOUNT ON

	INSERT INTO @ttblOwnerType
		(
		[Code],
		[Description]
		)
	SELECT
		[Code],
		[Description]
	FROM
		luOwnerType
	ORDER BY
		[Description]

	SELECT
		[ID],
		[Code],
		[Description]
	FROM
		@ttblOwnerType

	SET NOCOUNT OFF

RETURN
