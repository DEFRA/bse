
CREATE PROCEDURE GetluTestType AS

DECLARE @ttblTestType TABLE
		(
		[ID] int IDENTITY(1, 1),
		[Code] varchar(10),
		[Description] varchar(50),
		[IsActive] bit
		)

	SET NOCOUNT ON

	INSERT INTO @ttblTestType
		(
		[Code],
		[Description],
		[IsActive]
		)
	SELECT
		[Code],
		[Description],
		[IsActive]
	FROM
		luTestType
	ORDER BY
		[Description]

	SELECT
		[ID],
		[Code],
		[Description],
		[IsActive]
	FROM
		@ttblTestType

	SET NOCOUNT OFF

RETURN
