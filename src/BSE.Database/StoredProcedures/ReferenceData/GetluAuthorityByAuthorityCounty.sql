
CREATE PROCEDURE GetluAuthorityByAuthorityCounty
	@AuthorityCountyID int AS

	SELECT
		[ID],
		[Name]
	FROM
		[luAuthority]
	WHERE
		[AuthorityCountyID] = @AuthorityCountyID
	ORDER BY
		[Name]
