set ANSI_NULLS ON
set QUOTED_IDENTIFIER ON
go


ALTER PROCEDURE [dbo].[GetSearchOutstandingBSE1s]
	@EarliestFormADate datetime,
	@LatestFormADate datetime,
	@IncludeNonGBCases bit = 0  AS

	SET NOCOUNT ON

	SELECT
		LEFT([Case].[RBSE], 2) + '/' + SUBSTRING([Case].[RBSE], 3, 2) + '/' + RIGHT([Case].[RBSE], 5) AS [RBSE],
		LEFT([Case].[CPHH], 2) + '/' + SUBSTRING([Case].[CPHH], 3, 3) + '/' + SUBSTRING([Case].[CPHH], 6, 4) + '/' + RIGHT([Case].[CPHH], 2) AS [CPHH],
		ISNULL([Case].[EartagCountry], '') + ISNULL([Case].[EartagHerdmark] + ' ', '') + ISNULL([Case].[Eartag], '') AS [Eartag],
		[FormADate],
		[BirthDate],
		[luCaseFate].[Description] AS [Fate],
		[luTestResult].[Description] AS [FinalResult]
	FROM
		[Case] LEFT JOIN [luCaseFate] ON [Case].[Fate] = [luCaseFate].[Code]
		LEFT JOIN [luTestResult] ON [Case].[FinalResult] = [luTestResult].[Code]
	WHERE
		([Case].[BSE1ReceivedDate] IS NULL) AND
		ISNULL([Case].[FormADate], '1 January 1900') BETWEEN ISNULL(@EarliestFormADate, '1 January 1900') AND ISNULL(@LatestFormADate, GETDATE()) AND
		([Case].[IsNonGBCase] = 0 OR [Case].[IsNonGBCase] = @IncludeNonGBCases)
	ORDER BY
		[Case].[RBSE]

	SET NOCOUNT OFF

 