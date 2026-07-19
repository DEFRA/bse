CREATE PROCEDURE [dbo].[GetSearchCase]
	@RBSE varchar(9) = '',
	@Eartag varchar(35) = '',
	@DBSE varchar(7) = '',
	@Fate varchar(4) = '',
	@FinalResult varchar(5) = '',
	@Sex varchar(1) = '',
	@Survey varchar(4) = '',
	@Notes varchar(500) = '',
	@EarliestFormADate datetime,
	@LatestFormADate datetime,
	@EarliestFinalResultDate datetime,
	@LatestFinalResultDate datetime,
	@EarliestBirthDate datetime,
	@LatestBirthDate datetime,
	@IncludeNonGBCases bit = 0,
	@PassiveActive varchar(1) = '',
	@IsImportedCase bit = 0  AS

	SET NOCOUNT ON

	SELECT
		LEFT([Case].[RBSE], 2) + '/' + SUBSTRING([Case].[RBSE], 3, 2) + '/' + RIGHT([Case].[RBSE], 5) AS [RBSE],
		LEFT([Case].[CPHH], 2) + '/' + SUBSTRING([Case].[CPHH], 3, 3) + '/' + SUBSTRING([Case].[CPHH], 6, 4) + '/' + RIGHT([Case].[CPHH], 2) AS [CPHH],
		[luSex].[Description] AS [Sex],
		[luSurvey].[Description] AS [Survey],
		ISNULL([Case].[EartagCountry], '') + ISNULL([Case].[EartagHerdmark] + ' ', '') + ISNULL([Case].[Eartag], '') AS [Eartag],
		[BirthDate],
		CASE [IsBirthDateEst] WHEN 1 THEN 'Y' ELSE 'N' END AS [IsBirthDateEst],
		[FormADate],
		[luCaseFate].[Description] AS [Fate],
		[luTestResult].[Description] AS [FinalResult],
		[FinalResultDate],
		LEFT([Case].[DBSE], 2) + '/' + RIGHT([Case].[DBSE], 5) AS [DBSE],
		[Case].[Notes],
		[CaseBAB].[Notes] AS [BabNotes],
		[luAnimalOrigin].[Description] AS [Origin],
		[luValuationAge].[Description] AS [ValuationAge]
	FROM
		[Case] LEFT JOIN [CaseBAB] ON [Case].[RBSE] = [CaseBAB].[RBSE]
		LEFT JOIN [luSex] ON [Case].[Sex] = [luSex].[Code]
		LEFT JOIN [luSurvey] ON [Case].[Survey] = [luSurvey].[Code]
		LEFT JOIN [luCaseFate] ON [Case].[Fate] = [luCaseFate].[Code]
		LEFT JOIN [luTestResult] ON [Case].[FinalResult] = [luTestResult].[Code]
		LEFT JOIN [luAnimalOrigin] ON [Case].[Origin] = [luAnimalOrigin].[Code]
		LEFT JOIN [luValuationAge] ON [Case].[ValuationAge] = [luValuationAge].[Code]
	WHERE
		[Case].[RBSE] LIKE @RBSE + '%' AND
		ISNULL([Case].[EartagCountry], '') + ISNULL([Case].[EartagHerdmark] + ' ', '') + ISNULL([Case].[Eartag], '') LIKE @Eartag + '%' AND
		ISNULL([Case].[DBSE], '') LIKE @DBSE + '%' AND
		ISNULL([Case].[Fate], '') LIKE @Fate + '%' AND
		ISNULL([Case].[FinalResult], '') LIKE @FinalResult + '%' AND
		ISNULL([Case].[Sex], '') LIKE @Sex + '%' AND
		ISNULL([Case].[Survey], '') LIKE @Survey + '%' AND
		ISNULL([Case].[Notes], '') LIKE '%' + @Notes + '%' AND
		ISNULL([Case].[FormADate], '1 January 1900') BETWEEN ISNULL(@EarliestFormADate, '1 January 1900') AND ISNULL(@LatestFormADate, GETDATE()) AND
		ISNULL([Case].[FinalResultDate], '1 January 1900') BETWEEN ISNULL(@EarliestFinalResultDate, '1 January 1900') AND ISNULL(@LatestFinalResultDate, GETDATE()) AND
		ISNULL([Case].[BirthDate], '1 January 1900') BETWEEN ISNULL(@EarliestBirthDate, '1 January 1900') AND ISNULL(@LatestBirthDate, GETDATE())  AND
		([Case].[IsNonGBCase] = 0 OR [Case].[IsNonGBCase] = @IncludeNonGBCases) AND
		((@PassiveActive = 'P' AND (ISNULL([Case].[Survey], 'PS') = 'PS')) OR
		(@PassiveActive = 'A' AND (ISNULL([Case].[Survey], 'PS') != 'PS')) OR
		(@PassiveActive = '')) AND
		(@IsImportedCase = 0 OR 
		(ISNULL([Case].[EartagCountry], '') != '' AND ISNULL([Case].[EartagCountry], '') != 'UK' AND ISNULL([Case].[IsoFormatEarTag], 0) != 1
		AND ISNULL([Case].[EartagCountry], '') != 'GB0' AND ISNULL([Case].[EartagCountry], '') != 'GB1'  AND ISNULL([Case].[EartagCountry], '') != 'GB2'
		AND ISNULL([Case].[EartagCountry], '') != '8260' AND ISNULL([Case].[EartagCountry], '') != '8261'  AND ISNULL([Case].[EartagCountry], '') != '8262'))
	ORDER BY
		[Case].[RBSE]

	SET NOCOUNT OFF
