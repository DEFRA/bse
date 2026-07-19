set ANSI_NULLS ON
set QUOTED_IDENTIFIER ON
go


ALTER PROCEDURE [dbo].[GetSearchCaseByCPHH]
	@CPHH varchar(11) = '',
	@Herdmark varchar(8) = '',
	@NumericHerdmark varchar(6) = '',
	@IncludeNonGBCases bit = 0 AS

	SET NOCOUNT ON

	IF @Herdmark = '' BEGIN
		SET @Herdmark = '%'
	END

	IF @NumericHerdmark = '' BEGIN
		SET @NumericHerdmark = '%'
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
		[FinalResultDate],
		[OnsetAgeInMonths],
		[luCaseFate].[Description] AS [Fate],
		[luTestResult].[Description] AS [FinalResult],
		[luSurvey].[Description] AS [Survey],
		CASE WHEN [Case].[FinalResult] = 'Pos' THEN 'Positive' WHEN [Case].[FinalResult] IS NULL AND [Case].[Fate] NOT IN ('Alt', 'Rec') THEN 'Pending' ELSE 'Negative' END AS [CaseStatus],
		CASE WHEN [Case].[FinalResult] = 'Pos' THEN dbo.udfYearsAndMonths([Case].[FinalResultDate], GETDATE()) WHEN [Case].[FinalResult] IS NULL AND [Case].[Fate] NOT IN ('Alt', 'Rec') THEN dbo.udfYearsAndMonths([Case].[FormADate], GETDATE()) ELSE NULL END  AS [TimeElapsed],
		CASE WHEN [case].[FinalResult] = 'Pos' THEN DATEDIFF(day, [Case].[FinalResultDate], GETDATE()) WHEN [Case].[FinalResult] IS NULL AND [Case].[Fate] NOT IN ('Alt', 'Rec') THEN DATEDIFF(day, [Case].[FormADate], GETDATE()) ELSE NULL END  AS [DaysElapsed],
		[luAnimalOrigin].[Description] AS [Origin]
	FROM
		[Case] INNER JOIN [Farm] ON [Case].[CPHH] = [Farm].[CPHH]
		LEFT JOIN [luSex] ON [Case].[Sex] = [luSex].[Code]
		LEFT JOIN [luSurvey] ON [Case].[Survey] = [luSurvey].[Code]
		LEFT JOIN [luCaseFate] ON [Case].[Fate] = [luCaseFate].[Code]
		LEFT JOIN [luTestResult] ON [Case].[FinalResult] = [luTestResult].[Code]
		LEFT JOIN [luAnimalOrigin] ON [Case].[Origin] = [luAnimalOrigin].[Code]
	WHERE
		[Case].[CPHH] LIKE @CPHH + '%' AND
		(ISNULL([Herdmark1],'') LIKE @Herdmark OR ISNULL([Herdmark2],'') LIKE @Herdmark OR ISNULL([Herdmark3],'') LIKE @Herdmark) AND
       		(ISNULL([NumericHerdmark1], '') LIKE @NumericHerdmark OR ISNULL([NumericHerdmark2], '') LIKE @NumericHerdmark) AND
		([Case].[IsNonGBCase] = 0 OR [Case].[IsNonGBCase] = @IncludeNonGBCases)
	ORDER BY
		[Case].[CPHH],
		[Case].[RBSE]

	SET NOCOUNT OFF
 