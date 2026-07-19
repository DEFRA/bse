set ANSI_NULLS ON
set QUOTED_IDENTIFIER ON
go


ALTER PROCEDURE [dbo].[GetRelationsByRBSE]
	@RBSE char(9)  AS

	SET NOCOUNT ON

	SELECT
		[CaseRelation].[ID],
		[CaseRelation].[RBSE],
		[RelationType],
		[luRelationType].[Description] AS [RelationTypeDesc],
		[RelationRBSE],
		ISNULL([Case].[Sex], [CaseRelation].[Sex]) AS [Sex],
		ISNULL([luCaseSex].[Description], [luRelationSex].[Description]) AS [SexDesc],
		ISNULL(DAY([Case].[BirthDate]), [CaseRelation].[BirthDay]) AS [BirthDay],
		ISNULL(MONTH([Case].[BirthDate]), [CaseRelation].[BirthMonth]) AS [BirthMonth],
		ISNULL(YEAR([Case].[BirthDate]), [CaseRelation].[BirthYear]) AS [BirthYear],
		ISNULL([Case].[Fate], [RelationFate]) AS [RelationFate],
		ISNULL([luCaseFate].[Description], [luRelationFate].[Description]) AS [RelationFateDesc],
		ISNULL([Case].[SlaughterDate], [LeftDate]) AS [LeftDate],
		ISNULL([Case].[EartagCountry], [CaseRelation].[EartagCountry]) AS [EartagCountry],
		ISNULL([Case].[EartagHerdmark], [CaseRelation].[EartagHerdmark]) AS [EartagHerdmark],
		ISNULL([Case].[Eartag], [CaseRelation].[Eartag]) AS [Eartag],
		ISNULL([sirePedigree].[Name], [CaseRelation].[Sire]) AS [Sire],
		[CaseRelation].[RowStamp]
	FROM
		[CaseRelation] INNER JOIN [luRelationType] ON [CaseRelation].[RelationType] = [luRelationType].[Code]
		LEFT JOIN [luSex] [luRelationSex] ON [CaseRelation].[Sex] = [luRelationSex].[Code]
		LEFT JOIN [luRelationFate] ON [CaseRelation].[RelationFate] = [luRelationFate].[Code]
		LEFT JOIN [Case] ON [CaseRelation].[RelationRBSE] = [Case].[RBSE]
		LEFT JOIN [luSex] [luCaseSex] ON [Case].[Sex] = [luCaseSex].[Code]
		LEFT JOIN [luCaseFate] ON [Case].[Fate] = [luCaseFate].[Code]
		LEFT JOIN [Pedigree] [casePedigree] ON [Case].[RBSE] = [casePedigree].[RBSE]
		LEFT JOIN [Pedigree] [sirePedigree] ON [casePedigree].[SireID] = [sirePedigree].[ID]
	WHERE
		[CaseRelation].[RBSE] = @RBSE

	SET NOCOUNT OFF

 