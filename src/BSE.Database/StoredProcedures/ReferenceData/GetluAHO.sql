
CREATE PROCEDURE GetluAHO AS

DECLARE @ttblAHO TABLE
		(
		[ID] int IDENTITY(1, 1),
		[Code] char(2),
		[Name] varchar(50),
		[BSERegionID] int
		)

	SET NOCOUNT ON

	INSERT INTO @ttblAHO
		(
		[Code],
		[Name],
		[BSERegionID]
		)
	SELECT
		[Code],
		[Name],
		[BSERegionID]
	FROM
		luAHO
	ORDER BY
		[Name]

	SELECT
		[ID],
		[Code],
		[Name],
		[BSERegionID]
	FROM
		@ttblAHO

	SET NOCOUNT OFF

RETURN
