

CREATE PROCEDURE [dbo].[GetDamSireDetailsMatches]
	@Eartag varchar(32),
	@Name varchar(80),
	@RBSE char(9),
	@Herdbook varchar(15),
	@Sex char(1) AS

	IF @Sex = 'F' BEGIN

		IF @RBSE IS NOT NULL BEGIN
	
			SELECT
				[Pedigree].[ID],
				[Case].[RBSE],
				ISNULL([Case].[EartagCountry], '') + ISNULL([Case].[EartagHerdmark] + ' ', '') + ISNULL([Case].[Eartag], '')  AS [Eartag],
				[Pedigree].[Name],
				[Pedigree].[Herdbook],
				CONVERT(tinyint, DAY([Case].[BirthDate])) AS [BirthDay],				CONVERT(tinyint, MONTH([Case].[BirthDate])) AS [BirthMonth],
				CONVERT(smallint, YEAR([Case].[BirthDate]) )AS [BirthYear],
				[luCaseFate].[Description] AS [Fate],
				[luTestResult].[Description] AS [FinalResult],
				[childPedigree].[ChildCount],
				[Pedigree].[RowStamp]
			FROM
				[Case] LEFT JOIN [luCaseFate] ON [Case].[Fate] = [luCaseFate].[Code]
				LEFT JOIN [luTestResult] ON [Case].[FinalResult] = [luTestResult].[Code] 
				LEFT JOIN [Pedigree] ON [Case].[RBSE] = [Pedigree].[RBSE]
				LEFT JOIN (SELECT COUNT(*) AS [ChildCount], [DamID] FROM [Pedigree] GROUP BY [DamID]) AS [childPedigree] ON [Pedigree].[ID] = [childPedigree].[DamID]
			WHERE
				[Case].[RBSE] = @RBSE AND
				ISNULL([Case].[Sex], [Pedigree].[Sex]) IN (NULL, 'F', 'U')
			ORDER BY
				[Pedigree].[Name]
	
		END ELSE BEGIN
	
			SELECT
				[Pedigree].[ID],
				[Pedigree].[RBSE],
				CASE
					WHEN [Pedigree].[RBSE] IS NULL THEN [Pedigree].[Eartag]
					ELSE ISNULL([Case].[EartagCountry], '') + ISNULL([Case].[EartagHerdmark] + ' ', '') + ISNULL([Case].[Eartag], '') 
				END AS [Eartag],
				[Pedigree].[Name],
				[Pedigree].[Herdbook],
				CASE
					WHEN [Pedigree].[RBSE] IS NULL THEN [Pedigree].[BirthDay]
					ELSE CONVERT(tinyint, DAY([Case].[BirthDate])) 
				END AS [BirthDay],				CASE
					WHEN [Pedigree].[RBSE] IS NULL THEN [Pedigree].[BirthMonth]
					ELSE CONVERT(tinyint, MONTH([Case].[BirthDate]))
				END AS [BirthMonth],
				CASE
					WHEN [Pedigree].[RBSE] IS NULL THEN [Pedigree].[BirthYear]
					ELSE CONVERT(smallint, YEAR([Case].[BirthDate]) )
				END AS [BirthYear],
				[luCaseFate].[Description] AS [Fate],
				[luTestResult].[Description] AS [FinalResult],
				[childPedigree].[ChildCount],
				[Pedigree].[RowStamp]
			FROM
				[Pedigree] LEFT JOIN [Case] ON [Pedigree].[RBSE] = [Case].[RBSE]
				LEFT JOIN [luCaseFate] ON [Case].[Fate] = [luCaseFate].[Code]
				LEFT JOIN [luTestResult] ON [Case].[FinalResult] = [luTestResult].[Code] 
				LEFT JOIN (SELECT COUNT(*) AS [ChildCount], [DamID] FROM [Pedigree] GROUP BY [DamID]) AS [childPedigree] ON [Pedigree].[ID] = [childPedigree].[DamID]
			WHERE
				CASE
					WHEN [Pedigree].[RBSE] IS NULL THEN ISNULL([Pedigree].[Eartag], '')
					ELSE ISNULL([Case].[EartagCountry], '') + ISNULL([Case].[EartagHerdmark] + ' ', '') + ISNULL([Case].[Eartag], '') 
				END  LIKE ISNULL(@Eartag, '') + '%' AND
				ISNULL([Pedigree].[Name], '') LIKE '%' + ISNULL(@Name, '') + '%' AND
				ISNULL([Case].[Sex], [Pedigree].[Sex]) IN (NULL,'F', 'U') AND
				ISNULL([Pedigree].[Herdbook], '') LIKE ISNULL(@Herdbook, '') + '%'
			ORDER BY
				[Pedigree].[Name]
	
		END

	END ELSE BEGIN


		IF @RBSE IS NOT NULL BEGIN
	
			SELECT
				[Pedigree].[ID],
				[Case].[RBSE],
				ISNULL([Case].[EartagCountry], '') + ISNULL([Case].[EartagHerdmark] + ' ', '') + ISNULL([Case].[Eartag], '')  AS [Eartag],
				[Pedigree].[Name],
				[Pedigree].[Herdbook],
				CONVERT(tinyint, DAY([Case].[BirthDate])) AS [BirthDay],				CONVERT(tinyint, MONTH([Case].[BirthDate])) AS [BirthMonth],
				CONVERT(smallint, YEAR([Case].[BirthDate]) )AS [BirthYear],
				[luCaseFate].[Description] AS [Fate],
				[luTestResult].[Description] AS [FinalResult],
				[childPedigree].[ChildCount],
				[Pedigree].[RowStamp]
			FROM
				[Case] LEFT JOIN [luCaseFate] ON [Case].[Fate] = [luCaseFate].[Code]
				LEFT JOIN [luTestResult] ON [Case].[FinalResult] = [luTestResult].[Code] 
				LEFT JOIN [Pedigree] ON [Case].[RBSE] = [Pedigree].[RBSE]
				LEFT JOIN (SELECT COUNT(*) AS [ChildCount], [SireID] FROM [Pedigree] GROUP BY [SireID]) AS [childPedigree] ON [Pedigree].[ID] = [childPedigree].[SireID]
			WHERE
				[Case].[RBSE] = @RBSE AND
				ISNULL([Case].[Sex], [Pedigree].[Sex]) IN (NULL, 'M', 'U')
			ORDER BY
				[Pedigree].[Name]
	
		END ELSE BEGIN
	
			SELECT
				[Pedigree].[ID],
				[Pedigree].[RBSE],
				CASE
					WHEN [Pedigree].[RBSE] IS NULL THEN [Pedigree].[Eartag]
					ELSE ISNULL([Case].[EartagCountry], '') + ISNULL([Case].[EartagHerdmark] + ' ', '') + ISNULL([Case].[Eartag], '') 
				END AS [Eartag],
				[Pedigree].[Name],
				[Pedigree].[Herdbook],
				CASE
					WHEN [Pedigree].[RBSE] IS NULL THEN [Pedigree].[BirthDay]
					ELSE CONVERT(tinyint, DAY([Case].[BirthDate])) 
				END AS [BirthDay],				CASE
					WHEN [Pedigree].[RBSE] IS NULL THEN [Pedigree].[BirthMonth]
					ELSE CONVERT(tinyint, MONTH([Case].[BirthDate]))
				END AS [BirthMonth],
				CASE
					WHEN [Pedigree].[RBSE] IS NULL THEN [Pedigree].[BirthYear]
					ELSE CONVERT(smallint, YEAR([Case].[BirthDate]) )
				END AS [BirthYear],
				[luCaseFate].[Description] AS [Fate],
				[luTestResult].[Description] AS [FinalResult],
				[childPedigree].[ChildCount],
				[Pedigree].[RowStamp]
			FROM
				[Pedigree] LEFT JOIN [Case] ON [Pedigree].[RBSE] = [Case].[RBSE]
				LEFT JOIN [luCaseFate] ON [Case].[Fate] = [luCaseFate].[Code]
				LEFT JOIN [luTestResult] ON [Case].[FinalResult] = [luTestResult].[Code] 
				LEFT JOIN (SELECT COUNT(*) AS [ChildCount], [SireID] FROM [Pedigree] GROUP BY [SireID]) AS [childPedigree] ON [Pedigree].[ID] = [childPedigree].[SireID]
			WHERE
				CASE
					WHEN [Pedigree].[RBSE] IS NULL THEN ISNULL([Pedigree].[Eartag], '')
					ELSE ISNULL([Case].[EartagCountry], '') + ISNULL([Case].[EartagHerdmark] + ' ', '') + ISNULL([Case].[Eartag], '') 
				END  LIKE ISNULL(@Eartag, '') + '%' AND
				ISNULL([Pedigree].[Name], '') LIKE '%' + ISNULL(@Name, '') + '%' AND
				ISNULL([Case].[Sex], [Pedigree].[Sex]) IN (NULL,'M', 'U') AND
				ISNULL([Pedigree].[Herdbook], '') LIKE ISNULL(@Herdbook, '') + '%'
			ORDER BY
				[Pedigree].[Name]
	
		END



	END

 
