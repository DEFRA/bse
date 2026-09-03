

CREATE PROCEDURE GetAuditLogByCase
	@RBSE char(9) AS

SELECT
		[AuditLog].[ID],
		[AuditLog].[TableName],
		[AuditLog].[FieldName],
		[AuditLog].[LogDate]  AS [DateTime],
		[User].[Name] AS [UserName],
		[AuditLog].[BeforeValue],
		[AuditLog].[AfterValue],
		[AuditLog].[Reason],
		ISNULL(CONVERT(varchar(20),LEFT([AuditLog].[RBSE], 2) + '/' + SUBSTRING([AuditLog].[RBSE], 3, 2) + '/' + RIGHT([AuditLog].[RBSE], 5)), CONVERT(varchar(20),LEFT([AuditLog].[CPHH], 2) + '/' + SUBSTRING([AuditLog].[CPHH], 3, 3) + '/' + SUBSTRING([AuditLog].[CPHH], 6, 4) + '/' + RIGHT([AuditLog].[CPHH], 2))) AS [Key]
	FROM
		[AuditLog] INNER JOIN [User] ON [AuditLog].[UserID] = [User].[ID]
	WHERE
		[RBSE] = @RBSE OR
		[BeforeValue] = @RBSE OR
		[AfterValue] = @RBSE
	ORDER BY
		[LogDate] DESC
