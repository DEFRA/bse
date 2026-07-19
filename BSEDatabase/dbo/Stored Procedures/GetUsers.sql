

CREATE PROCEDURE GetUsers AS

	SELECT
		[ID],
		[NTLogin],
		[Name],
		[Email],
		[UserGroup],
		[IsActive]
	FROM
		[User]
	ORDER BY
		[Name]
