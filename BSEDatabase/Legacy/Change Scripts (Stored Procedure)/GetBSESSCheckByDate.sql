set ANSI_NULLS ON
set QUOTED_IDENTIFIER ON
go


ALTER PROCEDURE [dbo].[GetBSESSCheckByDate]
	@StartDate datetime,
	@EndDate datetime AS

	SELECT
		LEFT([BSESSImport].[RBSE], 2) + '/' + SUBSTRING([BSESSImport].[RBSE], 3, 2) + '/' + RIGHT([BSESSImport].[RBSE], 5) AS [RBSE],
		[BSESSImport].[BirthDate] AS [BSESSBirthDate],
		[Case].[BirthDate] AS [BSEBirthDate],
		[BSESSImport].[Eartag] AS [BSESSEartag],
		ISNULL([Case].[EartagCountry], '') + ISNULL([Case].[EartagHerdmark] + ' ', '') + ISNULL([Case].[Eartag], '') AS [BSEEartag],
		[BSESSImport].[TestGroupName] AS [BSESSTestGroup],
		[luSurvey].[Description] AS [BSETestGroup]
	FROM
		[BSESSImport] LEFT JOIN [Case] ON [BSESSImport].[RBSE] = [Case].[RBSE]
		LEFT JOIN [luSurvey] ON [Case].[Survey] = [luSurvey].[Code]
	WHERE
		ISNULL([BSESSImport].[NotificationDate], '1 January 1900') BETWEEN ISNULL(@StartDate, '1 January 1900') AND ISNULL(@EndDate, GETDATE()) AND
		((([BSESSImport].[BirthDate] != [Case].[BirthDate]) OR ([BSESSImport].[BirthDate] IS NULL AND [Case].[BirthDate] IS NOT NULL) OR ([BSESSImport].[BirthDate] IS NOT NULL AND [Case].[BirthDate] IS NULL)) OR
		(ISNULL([BSESSImport].[Eartag], '') != ISNULL([Case].[EartagCountry], '') + ISNULL([Case].[EartagHerdmark] + '-', '') + ISNULL([Case].[Eartag], '')) OR
		(ISNULL([BSESSImport].[TestGroupDerivedSurvey], '') != ISNULL([Case].[Survey], '')))
	ORDER BY
		[NotificationDate]

 