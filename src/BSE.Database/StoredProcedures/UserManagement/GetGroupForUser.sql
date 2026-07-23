
CREATE PROCEDURE GetGroupForUser 
	@NTLogin varchar(25)
AS
SELECT
	[User].[UserGroup],
	[luUserGroup].[Name]
FROM
	[User] INNER JOIN [luUserGroup] ON [User].[UserGroup] = [luUserGroup].[ID]
WHERE
	NTLogin = @NTLogin
