CREATE PROCEDURE GetUserByEmail
	@Email VARCHAR(60)
AS
SELECT
	[User].[ID],
	[User].[NTLogin],
	[User].[Name],
	[User].[UserGroup],
	[luUserGroup].[Name] AS GroupName,
	[User].[Email]
FROM
	[User] INNER JOIN [luUserGroup] ON [User].[UserGroup] = [luUserGroup].[ID]
WHERE
	[User].[Email] = @Email AND [User].[IsActive] = 1

