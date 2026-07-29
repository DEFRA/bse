
CREATE PROCEDURE GetluReportedLocation AS

DECLARE @ttblReportedLocation TABLE
		(
		[ID] int IDENTITY(1, 1),
		[Code] varchar(5),
		[Description] varchar(50)
		)

	SET NOCOUNT ON

	INSERT INTO @ttblReportedLocation
		(
		[Code],
		[Description]
		)
	SELECT
		[Code],
		[Description]
	FROM
		luReportedLocation
	ORDER BY
		[Description]

	SELECT
		[ID],
		[Code],
		[Description]
	FROM
		@ttblReportedLocation

	SET NOCOUNT OFF

RETURN
