
CREATE PROCEDURE GetluBSECounty AS

DECLARE @ttblBSECounty TABLE
		(
		[ID] int IDENTITY(1, 1),
		[IDColumn] char(2),
		[Code] varchar(15),
		[Description] varchar(50),
		[BSERegionID] int
		)

	SET NOCOUNT ON

	INSERT INTO @ttblBSECounty
		(
		[IDColumn],
		[Code],
		[Description],
		[BSERegionID]
		)
	SELECT
		[ID],
		[Code],
		[Description],
		[BSERegionID]
	FROM
		luBSECounty
	ORDER BY
		[Description]

	SELECT
		[ID],
		[IDColumn],
		[Code],
		[Description],
		[BSERegionID]
	FROM
		@ttblBSECounty

	SET NOCOUNT OFF

RETURN
