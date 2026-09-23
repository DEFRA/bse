SET ANSI_NULLS OFF
GO
 
SET QUOTED_IDENTIFIER ON
GO
 

-- Idempotent: DROP + CREATE ensures no conflicts on re-deployment
IF OBJECT_ID('[dbo].[GetUserByEmail]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[GetUserByEmail];
GO

CREATE OR ALTER PROCEDURE [dbo].[GetUserByEmail]
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
 
 
GO

