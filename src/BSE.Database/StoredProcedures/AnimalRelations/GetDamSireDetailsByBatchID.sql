

CREATE PROCEDURE [dbo].[GetDamSireDetailsByBatchID]
	@BatchID int AS

		SELECT
			-- columns for the Case
			[Case].[RBSE],
			LEFT([Case].[RBSE], 2) + '/' + SUBSTRING([Case].[RBSE], 3, 2) + '/' + RIGHT([Case].[RBSE], 5) AS [DisplayRBSE],
			[Case].[CPHH],
			LEFT([Case].[CPHH], 2) + '/' + SUBSTRING([Case].[CPHH], 3, 3) + '/' + SUBSTRING([Case].[CPHH], 6, 4) + '/' + RIGHT([Case].[CPHH], 2) AS [DisplayCPHH],
			-- columns for the Sire
			[sirePedigree].[ID] AS [SireID],
			[sirePedigree].[RBSE] AS [SireRBSE],
			LEFT([sirePedigree].[RBSE], 2) + '/' + SUBSTRING([sirePedigree].[RBSE], 3, 2) + '/' + RIGHT([sirePedigree].[RBSE], 5) AS [SireDisplayRBSE],
			CASE WHEN [sirePedigree].[RBSE] IS NULL THEN [sirePedigree].[Eartag] ELSE ISNULL([sireCase].[EartagCountry], '') + ISNULL([sireCase].[EartagHerdmark] + ' ', '') + ISNULL([sireCase].[Eartag], '') END AS [SireEartag],
			[sirePedigree].[Name] AS [SireName],
			ISNULL(ISNULL(CONVERT(varchar(5), DAY([sireCase].[BirthDate])), [sirePedigree].[BirthDay]) + '/', '') + ISNULL(ISNULL(CONVERT(varchar(5), MONTH([sireCase].[BirthDate])), [sirePedigree].[BirthMonth]) + '/', '') + ISNULL(CONVERT(varchar(5), YEAR([sireCase].[BirthDate])), [sirePedigree].[BirthYear]) AS [SireBirthDate],
			[sireCaseFate].[Description] AS [SireFate],
			[sirePedigree].[Herdbook] AS [SireHerdbook],
			[sirePedigree].[AlternativeName] AS [SireAlternativeName],
			-- columns for the Dam
			[damPedigree].[ID] AS [DamID],
			[damPedigree].[RBSE] AS [DamRBSE],
			LEFT([damPedigree].[RBSE], 2) + '/' + SUBSTRING([damPedigree].[RBSE], 3, 2) + '/' + RIGHT([damPedigree].[RBSE], 5) AS [DamDisplayRBSE],
			CASE WHEN [damPedigree].[RBSE] IS NULL THEN [damPedigree].[Eartag] ELSE ISNULL([damCase].[EartagCountry], '') + ISNULL([damCase].[EartagHerdmark] + ' ', '') + ISNULL([damCase].[Eartag], '') END AS [DamEartag],
			[damPedigree].[Name] AS [DamName],
			ISNULL(ISNULL(CONVERT(varchar(5), DAY([damCase].[BirthDate])), [damPedigree].[BirthDay]) + '/', '') + ISNULL(ISNULL(CONVERT(varchar(5), MONTH([damCase].[BirthDate])), [damPedigree].[BirthMonth]) + '/', '') + ISNULL(CONVERT(varchar(5), YEAR([damCase].[BirthDate])), [damPedigree].[BirthYear]) AS [DamBirthDate],
			[damCaseFate].[Description] AS [DamFate],
			[damStatus].[Description] AS [DamStatus],
			[damPedigree].[Herdbook] AS [DamHerdbook],
			[damPedigree].[AlternativeName] AS [DamAlternativeName]
		FROM
			-- joins for the case
			[lnkBatchCase] INNER JOIN [Case] ON [lnkBatchCase].[RBSE] = [Case].[RBSE]
			LEFT JOIN [luAnimalStatus] [damStatus] ON [Case].[DamStatus] = [damStatus].[Code]
			LEFT JOIN [Pedigree] [casePedigree] ON [Case].[RBSE] = [casePedigree].[RBSE]
			-- joins for the sire
			LEFT JOIN [Pedigree] [sirePedigree] ON [sirePedigree].[ID] = [casePedigree].[SireID]
			LEFT JOIN [Case] [sireCase] ON [sirePedigree].[RBSE] = [sireCase].[RBSE]
			LEFT JOIN [luCaseFate] [sireCaseFate] ON [sireCase].[Fate] = [sireCaseFate].[Code]
			-- joins for the dam
			LEFT JOIN [Pedigree] [damPedigree] ON [damPedigree].[ID] = [casePedigree].[DamID]
			LEFT JOIN [Case] [damCase] ON [damPedigree].[RBSE] = [damCase].[RBSE]
			LEFT JOIN [luCaseFate] [damCaseFate] ON [damCase].[Fate] = [damCaseFate].[Code]
		WHERE
			[lnkBatchCase].[BatchID] =  @BatchID

 
