
CREATE PROCEDURE GetUserByNTLogin 
	@NTLogin varchar(25)
AS
SELECT
	[User].[ID],
	[User].[Name],
	[User].[UserGroup],
	[luUserGroup].[Name] AS GroupName,
	[User].[Email]
FROM
	[User] INNER JOIN [luUserGroup] ON [User].[UserGroup] = [luUserGroup].[ID]
WHERE
	NTLogin = @NTLogin AND [User].[IsActive] = 1
