set ANSI_NULLS ON
set QUOTED_IDENTIFIER ON
go


ALTER PROCEDURE [dbo].[GetSearchCaseByEartagHerdmark]
	@EartagHerdmark varchar(8),
	@IncludeNonGBCases bit = 0 AS

	SET NOCOUNT ON

	IF @EartagHerdmark = '' BEGIN
		SET @EartagHerdmark = '%'
	END

	SELECT
		LEFT([Case].[RBSE], 2) + '/' + SUBSTRING([Case].[RBSE], 3, 2) + '/' + RIGHT([Case].[RBSE], 5) AS [RBSE],
		LEFT([Case].[CPHH], 2) + '/' + SUBSTRING([Case].[CPHH], 3, 3) + '/' + SUBSTRING([Case].[CPHH], 6, 4) + '/' + RIGHT([Case].[CPHH], 2) AS [CPHH],
		[luSex].[Description] AS [Sex],
		ISNULL([Case].[EartagCountry], '') + ISNULL([Case].[EartagHerdmark] + ' ', '') + ISNULL([Case].[Eartag], '') AS [Eartag],
		[BirthDate],
		[PurchaseDate],
		[PurchaseAgeInMonths],
		[OnsetDate],
		[FormADate],
		[SlaughterDate],
		[OnsetAgeInMonths],
		[luCaseFate].[Description] AS [Fate],
		[luTestResult].[Description] AS [FinalResult],
		[luSurvey].[Description] AS [Survey],
		CASE WHEN [Case].[FinalResult] = 'Pos' THEN 'Positive' WHEN [Case].[FinalResult] IS NULL AND [Case].[Fate] NOT IN ('Alt', 'Rec') THEN 'Pending' ELSE 'Negative' END AS [CaseStatus],
		CASE WHEN [Case].[FinalResult] = 'Pos' THEN dbo.udfYearsAndMonths([Case].[FinalResultDate], GETDATE()) WHEN [Case].[FinalResult] IS NULL AND [Case].[Fate] NOT IN ('Alt', 'Rec') THEN dbo.udfYearsAndMonths([Case].[FormADate], GETDATE()) ELSE NULL END  AS [TimeElapsed],
		CASE WHEN [Case].[FinalResult] = 'Pos' THEN DATEDIFF(day, [Case].[FinalResultDate], GETDATE()) WHEN [Case].[FinalResult] IS NULL AND [Case].[Fate] NOT IN ('Alt', 'Rec') THEN DATEDIFF(day, [Case].[FormADate], GETDATE()) ELSE NULL END  AS [DaysElapsed],
		[luAnimalOrigin].[Description] AS [Origin],
		[Case].[FinalResultDate] AS [FinalResultDate]
	FROM
		[Case] LEFT JOIN [luSex] ON [Case].[Sex] = [luSex].[Code]
		LEFT JOIN [luSurvey] ON [Case].[Survey] = [luSurvey].[Code]
		LEFT JOIN [luCaseFate] ON [Case].[Fate] = [luCaseFate].[Code]
		LEFT JOIN [luTestResult] ON [Case].[FinalResult] = [luTestResult].[Code]
		LEFT JOIN [luAnimalOrigin] ON [Case].[Origin] = [luAnimalOrigin].[Code]
	WHERE
		[EartagCountry] + [EartagHerdmark] LIKE @EartagHerdmark AND
		([Case].[IsNonGBCase] = 0 OR [Case].[IsNonGBCase] = @IncludeNonGBCases)
	ORDER BY
		[DaysElapsed],
		[Case].[RBSE]

	SET NOCOUNT OFF
 