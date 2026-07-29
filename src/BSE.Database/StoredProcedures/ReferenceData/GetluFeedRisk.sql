
CREATE PROCEDURE GetluFeedRisk AS

DECLARE @ttblFeedRisk TABLE
		(
		[ID] int IDENTITY(1, 1),
		[Code] varchar(10),
		[Description] varchar(50)
		)

	SET NOCOUNT ON

	INSERT INTO @ttblFeedRisk
		(
		[Code],
		[Description]
		)
	SELECT
		[Code],
		[Description]
	FROM
		luFeedRisk
	ORDER BY
		[Description]

	SELECT
		[ID],
		[Code],
		[Description]
	FROM
		@ttblFeedRisk

	SET NOCOUNT OFF

RETURN
