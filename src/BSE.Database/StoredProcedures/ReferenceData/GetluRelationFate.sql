
CREATE PROCEDURE GetluRelationFate AS

DECLARE @ttblRelationFate TABLE
		(
		[ID] int IDENTITY(1, 1),
		[Code] varchar(12),
		[Description] varchar(50),
		[IsActive] bit
		)

	SET NOCOUNT ON

	INSERT INTO @ttblRelationFate
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
		luRelationFate
	ORDER BY
		[Description]

	SELECT
		[ID],
		[Code],
		[Description],
		[IsActive]
	FROM
		@ttblRelationFate

	SET NOCOUNT OFF

RETURN
