
CREATE PROCEDURE GetluSurvey AS

DECLARE @ttblSurvey TABLE
		(
		[ID] int IDENTITY(1, 1),
		[Code] varchar(4),
		[Description] varchar(50)
		)

	SET NOCOUNT ON

	INSERT INTO @ttblSurvey
		(
		[Code],
		[Description]
		)
	SELECT
		[Code],
		[Description]
	FROM
		luSurvey
	ORDER BY
		[Description]

	SELECT
		[ID],
		[Code],
		[Description]
	FROM
		@ttblSurvey

	SET NOCOUNT OFF

RETURN
