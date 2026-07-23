

CREATE PROCEDURE [dbo].[GetRelationDetailsOfRelatedCase]
	@RBSE char(9)  AS

	SET NOCOUNT ON

	SELECT
		[Case].[RBSE] AS [RelationRBSE],
		[Case].[Sex],
		[luCaseSex].[Description] AS [SexDesc],
		DAY([Case].[BirthDate]) AS [BirthDay],
		MONTH([Case].[BirthDate]) AS [BirthMonth],
		YEAR([Case].[BirthDate]) AS [BirthYear],
		[Case].[Fate],
		[luCaseFate].[Description] AS [FateDesc],
		CONVERT(varchar(30), [Case].[SlaughterDate], 103) AS [LeftDate],
		[Case].[EartagCountry],
		[Case].[EartagHerdmark],
		[Case].[Eartag],
		[SirePedigree].[Name]
	FROM
		[Case] LEFT JOIN [luSex] [luCaseSex] ON [Case].[Sex] = [luCaseSex].[Code]
		LEFT JOIN [luCaseFate] ON [Case].[Fate] = [luCaseFate].[Code]
		LEFT JOIN [Pedigree] [casePedigree] ON [Case].[RBSE] = [casePedigree].[RBSE]
		LEFT JOIN [Pedigree] [sirePedigree] ON [casePedigree].[SireID] = [sirePedigree].[ID]
	WHERE
		[Case].[RBSE] = @RBSE

	SET NOCOUNT OFF
 
