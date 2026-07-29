

CREATE PROCEDURE GetluUserGroup AS

SELECT
	[ID],
	[Name]
FROM
	luUserGroup
ORDER BY
	[Name]

RETURN

