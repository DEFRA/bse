
CREATE PROCEDURE GetluBSERegion AS

SELECT
	[ID],
	[SortOrder],
	[Name],
	[CountryID]
FROM
	luBSERegion
ORDER BY
	[Name]

RETURN
