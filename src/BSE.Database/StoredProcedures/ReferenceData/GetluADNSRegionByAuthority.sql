
CREATE PROCEDURE GetluADNSRegionByAuthority
	@AuthorityID int AS

	SELECT
		[ID],
		[Name]
	FROM
		[luADNSRegion]
	WHERE
		[AuthorityID] = @AuthorityID
	ORDER BY
		[Name]
