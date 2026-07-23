
CREATE PROCEDURE GetluRelationType AS

DECLARE @ttblRelationType TABLE
		(
		[ID] int IDENTITY(1, 1),
		[Code] varchar(11),
		[Description] varchar(50)
		)

	SET NOCOUNT ON

	INSERT INTO @ttblRelationType
		(
		[Code],
		[Description]
		)
	SELECT
		[Code],
		[Description]
	FROM
		luRelationType
	ORDER BY
		[Description]

	SELECT
		[ID],
		[Code],
		[Description]
	FROM
		@ttblRelationType

	SET NOCOUNT OFF

RETURN
