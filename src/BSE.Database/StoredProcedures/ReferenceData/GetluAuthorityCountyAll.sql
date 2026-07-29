
CREATE PROCEDURE GetluAuthorityCountyAll AS

	SELECT
		[ID],
		[County]
	FROM
		[luAuthorityCounty]
	ORDER BY
		[County]
