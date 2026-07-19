
CREATE PROCEDURE GetNonGBCounty AS

SELECT
	[ID],
	[Code],
	[Description],
	[BSERegionID]
FROM
	luBSECounty
WHERE
	[BSERegionID] IS NULL
ORDER BY
	[Description]

RETURN
