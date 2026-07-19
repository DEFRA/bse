
CREATE PROCEDURE GetluHerdType AS

DECLARE @ttblHerdType TABLE
		(
		[ID] int IDENTITY(1, 1),
		[Code] char(1),
		[Description] varchar(50)
		)

	SET NOCOUNT ON

	INSERT INTO @ttblHerdType
		(
		[Code],
		[Description]
		)
	SELECT
		[Code],
		[Description]
	FROM
		luHerdType
	ORDER BY
		[Description]

	SELECT
		[ID],
		[Code],
		[Description]
	FROM
		@ttblHerdType

	SET NOCOUNT OFF

RETURN
