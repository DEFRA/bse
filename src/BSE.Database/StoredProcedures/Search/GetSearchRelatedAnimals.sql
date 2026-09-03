


CREATE PROCEDURE [dbo].[GetSearchRelatedAnimals]
	@RBSE varchar(9),
	@Name varchar(80),
	@Eartag varchar(48),
	@RelationRBSE varchar(9),
	@RelationType varchar(11) AS

	SET NOCOUNT ON

	DECLARE @ttblRelatedAnimals TABLE
		(
		RBSE varchar(11),
		CPHH varchar(14),
		RelationType varchar(50),
		RelSex varchar(50),
		Eartag varchar(80),
		RelBirthDate varchar(30),
		RelFate varchar(50),
		LeftDate datetime,
		RelName varchar(80),
		RelEartag varchar(80),
		RelationRBSE varchar(11)
		)
	
	INSERT INTO @ttblRelatedAnimals
	SELECT
		LEFT([CaseRelation].[RBSE], 2) + '/' + SUBSTRING([CaseRelation].[RBSE], 3, 2) + '/' + RIGHT([CaseRelation].[RBSE], 5) AS [RBSE],
		LEFT([Case].[CPHH], 2) + '/' + SUBSTRING([Case].[CPHH], 3, 3) + '/' + SUBSTRING([Case].[CPHH], 6, 4) + '/' + RIGHT([Case].[CPHH], 2) AS [CPHH],
		[luRelationType].[Description] AS [RelationType],
		ISNULL([RelatedCaseSex].[Description], [luSex].[Description]) AS [RelSex],
		ISNULL([Case].[EartagCountry], '') + ISNULL([Case].[EartagHerdmark] + ' ', '') + ISNULL([Case].[Eartag], '') AS [Eartag],
		ISNULL(CONVERT(varchar(30), [RelatedCase].[BirthDate], 103), ISNULL(CONVERT(varchar(2),[CaseRelation].[BirthDay]) +'/', '') + ISNULL(convert(varchar(2),[CaseRelation].[BirthMonth]) + '/', '') + CONVERT(varchar(4),[CaseRelation].[BirthYear])) AS [RelBirthDate],
		ISNULL([luCaseFate].[Description], [luRelationFate].[Description]) AS [RelFate],
		ISNULL([RelatedCase].[SlaughterDate], [CaseRelation].[LeftDate]) AS [LeftDate],
		NULL, -- no name for offspring
		CASE WHEN [RelatedCase].[RBSE] IS NOT NULL THEN ISNULL([RelatedCase].[EartagCountry], '') + ISNULL([RelatedCase].[EartagHerdmark] + ' ', '') + ISNULL([RelatedCase].[Eartag], '') ELSE ISNULL([CaseRelation].[EartagCountry], '') + ISNULL([CaseRelation].[EartagHerdmark] + ' ', '') + ISNULL([CaseRelation].[Eartag], '') END AS [RelEartag],
		LEFT([CaseRelation].[RelationRBSE], 2) + '/' + SUBSTRING([CaseRelation].[RelationRBSE], 3, 2) + '/' + RIGHT([CaseRelation].[RelationRBSE], 5) AS [RelationRBSE]
	FROM
		[CaseRelation] INNER JOIN [Case] ON [CaseRelation].[RBSE] = [Case].[RBSE]
		INNER JOIN [luRelationType] ON [CaseRelation].[RelationType] = [luRelationType].[Code] 
		LEFT JOIN [luSex] ON [CaseRelation].[Sex] = [luSex].[Code]
		LEFT JOIN [luRelationFate] ON [CaseRelation].[RelationFate] = [luRelationFate].[Code]
		LEFT JOIN [Case] [RelatedCase] ON [CaseRelation].[RelationRBSE] = [RelatedCase].[RBSE]
		LEFT JOIN [luSex] [RelatedCaseSex] ON [RelatedCase].[Sex] = [RelatedCaseSex].[Code]
		LEFT JOIN [luCaseFate] ON [RelatedCase].[Fate] = [luCaseFate].[Code]
	WHERE
		[CaseRelation].[RBSE] LIKE @RBSE + '%' AND
		@Name = '' AND
		CASE WHEN [RelatedCase].[RBSE] IS NOT NULL THEN ISNULL([RelatedCase].[EartagCountry], '') + ISNULL([RelatedCase].[EartagHerdmark] + ' ', '') + ISNULL([RelatedCase].[Eartag], '') ELSE ISNULL([CaseRelation].[EartagCountry], '') + ISNULL([CaseRelation].[EartagHerdmark] + ' ', '') + ISNULL([CaseRelation].[Eartag], '') END LIKE @Eartag + '%' AND
		ISNULL([CaseRelation].[RelationRBSE], '') LIKE @RelationRBSE + '%' AND
		[CaseRelation].[RelationType] LIKE @RelationType + '%' 
		

	INSERT INTO @ttblRelatedAnimals
	SELECT
		LEFT([CasePedigree].[RBSE], 2) + '/' + SUBSTRING([CasePedigree].[RBSE], 3, 2) + '/' + RIGHT([CasePedigree].[RBSE], 5) AS [RBSE],
		LEFT([Case].[CPHH], 2) + '/' + SUBSTRING([Case].[CPHH], 3, 3) + '/' + SUBSTRING([Case].[CPHH], 6, 4) + '/' + RIGHT([Case].[CPHH], 2) AS [CPHH],
		'Dam' AS [RelationType],
		ISNULL([DamCaseSex].[Description],[luSex].[Description]) AS [RelSex],
		ISNULL([Case].[EartagCountry], '') + ISNULL([Case].[EartagHerdmark] + ' ', '') + ISNULL([Case].[Eartag], '') AS [Eartag],
		ISNULL(CONVERT(varchar(30), [DamCase].[BirthDate], 103), ISNULL(CONVERT(varchar(2),[DamPedigree].[BirthDay]) +'/', '') + ISNULL(CONVERT(varchar(2),[DamPedigree].[BirthMonth]) + '/', '') + CONVERT(varchar(4),[DamPedigree].[BirthYear])) AS [RelBirthDate],
		[luCaseFate].[Description] AS [RelFate],
		[DamCase].[SlaughterDate]  AS [LeftDate],
		[DamPedigree].[Name],
		CASE WHEN [DamCase].[RBSE] IS NOT NULL THEN  ISNULL([DamCase].[EartagCountry], '') + ISNULL([DamCase].[EartagHerdmark] + ' ', '') + ISNULL([DamCase].[Eartag], '') ELSE [DamPedigree].[Eartag] END AS [Eartag],
		LEFT([DamPedigree].[RBSE], 2) + '/' + SUBSTRING([DamPedigree].[RBSE], 3, 2) + '/' + RIGHT([DamPedigree].[RBSE], 5) AS [RelationRBSE]
	FROM
		[Pedigree] [CasePedigree] INNER JOIN [Pedigree] [DamPedigree] ON [CasePedigree].[DamID] = [DamPedigree].[ID]
		INNER JOIN [Case] ON [CasePedigree].[RBSE] = [Case].[RBSE]
		LEFT JOIN [luSex] ON [DamPedigree].[Sex] = [luSex].[Code]
		LEFT JOIN [Case] [DamCase] ON [DamPedigree].[RBSE] = [DamCase].[RBSE]
		LEFT JOIN [luSex] [DamCaseSex] ON [DamCase].[Sex] = [DamCaseSex].[Code]
		LEFT JOIN [luCaseFate] ON [DamCase].[Fate] = [luCaseFate].[Code]
	WHERE
		[CasePedigree].[RBSE] LIKE @RBSE + '%' AND
		ISNULL([DamPedigree].[Name], '') LIKE @Name + '%' AND
		CASE WHEN [DamCase].[RBSE] IS NOT NULL THEN  ISNULL([DamCase].[EartagCountry], '') + ISNULL([DamCase].[EartagHerdmark] + ' ', '') + ISNULL([DamCase].[Eartag], '') ELSE ISNULL([DamPedigree].[Eartag], '') END LIKE @Eartag + '%' AND
		ISNULL([DamPedigree].[RBSE], '') LIKE @RelationRBSE + '%' AND
		(@RelationType = 'DAM' OR @RelationType = '')
	
	INSERT INTO @ttblRelatedAnimals
	SELECT
		LEFT([CasePedigree].[RBSE], 2) + '/' + SUBSTRING([CasePedigree].[RBSE], 3, 2) + '/' + RIGHT([CasePedigree].[RBSE], 5) AS [RBSE],
		LEFT([Case].[CPHH], 2) + '/' + SUBSTRING([Case].[CPHH], 3, 3) + '/' + SUBSTRING([Case].[CPHH], 6, 4) + '/' + RIGHT([Case].[CPHH], 2) AS [CPHH],
		'Sire' AS [RelationType],
		ISNULL([SireCaseSex].[Description],[luSex].[Description]) AS [RelSex],
		ISNULL([Case].[EartagCountry], '') + ISNULL([Case].[EartagHerdmark]+ ' ', '') + ISNULL([Case].[Eartag], '') AS [Eartag],
		ISNULL(CONVERT(varchar(30), [SireCase].[BirthDate], 103), ISNULL(CONVERT(varchar(2),[SirePedigree].[BirthDay]) +'/', '') + ISNULL(CONVERT(varchar(2),[SirePedigree].[BirthMonth]) + '/', '') + CONVERT(varchar(4),[SirePedigree].[BirthYear])) AS [RelBirthDate],
		[luCaseFate].[Description] AS [RelFate],
		[SireCase].[SlaughterDate] AS [LeftDate],
		[SirePedigree].[Name],
		CASE WHEN [SireCase].[RBSE] IS NOT NULL THEN  ISNULL([SireCase].[EartagCountry], '') + ISNULL([SireCase].[EartagHerdmark] + ' ', '') + ISNULL([SireCase].[Eartag], '') ELSE [SirePedigree].[Eartag] END AS [Eartag],
		LEFT([SirePedigree].[RBSE], 2) + '/' + SUBSTRING([SirePedigree].[RBSE], 3, 2) + '/' + RIGHT([SirePedigree].[RBSE], 5) AS [RelationRBSE]
	FROM
		[Pedigree] [CasePedigree] INNER JOIN [Pedigree] [SirePedigree] ON [CasePedigree].[SireID] = [SirePedigree].[ID]
		INNER JOIN [Case] ON [CasePedigree].[RBSE] = [Case].[RBSE]
		LEFT JOIN [luSex] ON [SirePedigree].[Sex] = [luSex].[Code]
		LEFT JOIN [Case] [SireCase] ON [SirePedigree].[RBSE] = [SireCase].[RBSE]
		LEFT JOIN [luSex] [SireCaseSex] ON [SireCase].[Sex] = [SireCaseSex].[Code]
		LEFT JOIN [luCaseFate] ON [SireCase].[Fate] = [luCaseFate].[Code]
	WHERE
		[CasePedigree].[RBSE] LIKE @RBSE + '%' AND
		ISNULL([SirePedigree].[Name], '') LIKE @Name + '%' AND
		CASE WHEN [SireCase].[RBSE] IS NOT NULL THEN  ISNULL([SireCase].[EartagCountry], '') + ISNULL([SireCase].[EartagHerdmark] + ' ', '') + ISNULL([SireCase].[Eartag], '') ELSE ISNULL([SirePedigree].[Eartag], '') END LIKE @Eartag + '%' AND
		ISNULL([SirePedigree].[RBSE], '') LIKE @RelationRBSE + '%' AND
		(@RelationType = 'SIRE' OR @RelationType = '')

	SELECT
		RBSE,
		CPHH,
		RelationType,
		RelSex,
		Eartag,
		RelBirthDate,
		RelFate,
		LeftDate,
		RelName,
		RelEartag,
		RelationRBSE 
	FROM
		@ttblRelatedAnimals

	SET NOCOUNT OFF

 
