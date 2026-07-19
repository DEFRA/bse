set ANSI_NULLS ON
set QUOTED_IDENTIFIER ON
go


ALTER PROCEDURE [dbo].[GetSireDetailsByRBSE]
	@RBSE char(9) AS

	DECLARE @SireRBSE char(9)

	SET NOCOUNT ON

	SELECT
		@SireRBSE = [sirePedigree].[RBSE]
	FROM
		[Pedigree] [sirePedigree] INNER JOIN [Pedigree] [casePedigree] ON [sirePedigree].[ID] = [casePedigree].[SireID]
	WHERE
		[casePedigree].[RBSE] = @RBSE

	IF @SireRBSE IS NULL BEGIN

		SELECT
			[sirePedigree].[ID],
			[sirePedigree].[RBSE],
			[sirePedigree].[Eartag],
			[sirePedigree].[Name],
			[sirePedigree].[Herdbook],
			[sirePedigree].[BirthDay],			[sirePedigree].[BirthMonth],
			[sirePedigree].[BirthYear],
			CONVERT(varchar(50),NULL) AS [Fate],
			[childPedigree].[ChildCount],
			[sirePedigree].[RowStamp]
		FROM
			[Pedigree] [sirePedigree] INNER JOIN [Pedigree] [casePedigree] ON [sirePedigree].[ID] = [casePedigree].[SireID]
			LEFT JOIN (SELECT COUNT(*) AS [ChildCount], [SireID] FROM [Pedigree] GROUP BY [SireID]) AS [childPedigree] ON [sirePedigree].[ID] = [childPedigree].[SireID]
		WHERE
			[casePedigree].[RBSE] = @RBSE

	END ELSE BEGIN

		SELECT
			[sirePedigree].[ID],
			[sirePedigree].[RBSE],
			ISNULL([sireCase].[EartagCountry], '') + ISNULL([sireCase].[EartagHerdmark] + ' ', '') + ISNULL([sireCase].[Eartag], '')  AS [Eartag],
			[sirePedigree].[Name],
			[sirePedigree].[Herdbook],
			CONVERT(tinyint, DAY([sireCase].[BirthDate])) AS [BirthDay],			CONVERT(tinyint, MONTH([sireCase].[BirthDate])) AS [BirthMonth],
			CONVERT(smallint, YEAR([sireCase].[BirthDate]) )AS [BirthYear],
			[luCaseFate].[Description] AS [Fate],
			[childPedigree].[ChildCount],
			[sirePedigree].[RowStamp]
		FROM
			Pedigree sirePedigree INNER JOIN Pedigree casePedigree ON sirePedigree.ID = casePedigree.SireID
			INNER JOIN [Case] sireCase ON [sirePedigree].[RBSE] = [sireCase].[RBSE]
			LEFT JOIN [luCaseFate] ON [sireCase].[Fate] = [luCaseFate].[Code]
			LEFT JOIN (SELECT COUNT(*) AS [ChildCount], [SireID] FROM [Pedigree] GROUP BY [SireID]) AS [childPedigree] ON [sirePedigree].[ID] = [childPedigree].[SireID]
		WHERE
			[casePedigree].[RBSE] = @RBSE

	END

	SET NOCOUNT OFF

 