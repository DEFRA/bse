set ANSI_NULLS ON
set QUOTED_IDENTIFIER ON
go


ALTER PROCEDURE [dbo].[GetDamDetailsByRBSE]
	@RBSE char(9) AS

	DECLARE @DamRBSE char(9)

	SET NOCOUNT ON

	SELECT
		@DamRBSE = [damPedigree].[RBSE]
	FROM
		[Pedigree] [damPedigree] INNER JOIN [Pedigree] [casePedigree] ON [damPedigree].[ID] = [casePedigree].[DamID]
	WHERE
		[casePedigree].[RBSE] = @RBSE

	IF @DamRBSE IS NULL BEGIN

		SELECT
			[damPedigree].[ID],
			[damPedigree].[RBSE],
			[damPedigree].[Eartag],
			[damPedigree].[Name],
			[damPedigree].[Herdbook],
			[damPedigree].[BirthDay],			[damPedigree].[BirthMonth],
			[damPedigree].[BirthYear],
			CONVERT(varchar(50),NULL) AS [Fate],
			CONVERT(varchar(50),NULL) AS [FinalResult],
			[childPedigree].[ChildCount],
			[damPedigree].[RowStamp]
		FROM
			[Pedigree] [damPedigree] INNER JOIN [Pedigree] [casePedigree] ON [damPedigree].[ID] = [casePedigree].[DamID]
			LEFT JOIN (SELECT COUNT(*) AS [ChildCount], [DamID] FROM [Pedigree] GROUP BY [DamID]) AS [childPedigree] ON [damPedigree].[ID] = [childPedigree].[DamID]
		WHERE
			[casePedigree].[RBSE] = @RBSE

	END ELSE BEGIN

		SELECT
			[damPedigree].[ID],
			[damPedigree].[RBSE],
			ISNULL([damCase].[EartagCountry], '') + ISNULL([damCase].[EartagHerdmark] + ' ', '') + ISNULL([damCase].[Eartag], '')  AS [Eartag],
			[damPedigree].[Name],
			[damPedigree].[Herdbook],
			CONVERT(tinyint, DAY([damCase].[BirthDate])) AS [BirthDay],			CONVERT(tinyint, MONTH([damCase].[BirthDate])) AS [BirthMonth],
			CONVERT(smallint, YEAR([damCase].[BirthDate]) )AS [BirthYear],
			[luCaseFate].[Description] AS [Fate],
			[luTestResult].[Description] AS [FinalResult],
			[childPedigree].[ChildCount],
			[damPedigree].[RowStamp]
		FROM
			Pedigree damPedigree INNER JOIN Pedigree casePedigree ON damPedigree.ID = casePedigree.DamID
			INNER JOIN [Case] damCase ON [damPedigree].[RBSE] = [damCase].[RBSE]
			LEFT JOIN [luCaseFate] ON [damCase].[Fate] = [luCaseFate].[Code]
			LEFT JOIN [luTestResult] ON [damCase].[FinalResult] = [luTestResult].[Code]
			LEFT JOIN (SELECT COUNT(*) AS [ChildCount], [DamID] FROM [Pedigree] GROUP BY [DamID]) AS [childPedigree] ON [damPedigree].[ID] = [childPedigree].[DamID]
		WHERE
			[casePedigree].[RBSE] = @RBSE

	END

	SET NOCOUNT OFF

 