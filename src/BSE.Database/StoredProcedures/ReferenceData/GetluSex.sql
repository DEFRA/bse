
CREATE PROCEDURE GetluSex AS

DECLARE @ttblSex TABLE
		(
		[ID] int IDENTITY(1, 1),
		[Code] char(1),
		[Description] varchar(50)
		)

	SET NOCOUNT ON

	INSERT INTO @ttblSex
		(
		[Code],
		[Description]
		)
	SELECT
		[Code],
		[Description]
	FROM
		luSex
	ORDER BY
		[Description]

	SELECT
		[ID],
		[Code],
		[Description]
	FROM
		@ttblSex

	SET NOCOUNT OFF

RETURN
