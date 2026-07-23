


CREATE PROCEDURE [dbo].[GetRelationsByBatchID] 
	@BatchID int
AS
	SET NOCOUNT ON

	SELECT
		[CaseRelation].[RBSE],
		[luRelationType].[Description] AS [RelationTypeDesc],
		[RelationRBSE],
		ISNULL([Case].[Sex], [CaseRelation].[Sex]) AS [Sex],
		ISNULL([luCaseSex].[Description], [luRelationSex].[Description]) AS [SexDesc],
		ISNULL(DAY([Case].[BirthDate]), [CaseRelation].[BirthDay]) AS [BirthDay],
		ISNULL(MONTH([Case].[BirthDate]), [CaseRelation].[BirthMonth]) AS [BirthMonth],
		ISNULL(YEAR([Case].[BirthDate]), [CaseRelation].[BirthYear]) AS [BirthYear],
		ISNULL(CONVERT(varchar(5), ISNULL(DAY([Case].[BirthDate]), [CaseRelation].[BirthDay]) ) + '/', '') + ISNULL(CONVERT(varchar(5), ISNULL(MONTH([Case].[BirthDate]), [CaseRelation].[BirthMonth])) + '/', '') + CONVERT(varchar(5), ISNULL(YEAR([Case].[BirthDate]), [CaseRelation].[BirthYear])) AS [BirthDate],
		ISNULL([Case].[Fate], [RelationFate]) AS [RelationFate],
		ISNULL([luCaseFate].[Description], [luRelationFate].[Description]) AS [RelationFateDesc],
		CONVERT(varchar(30), ISNULL([Case].[SlaughterDate], [LeftDate]), 103) AS [LeftDate],
		ISNULL([Case].[EartagCountry] + [Case].[EartagHerdmark], [CaseRelation].[EartagCountry] + [CaseRelation].[EartagHerdmark]) AS [EartagHerdmark],
		ISNULL([Case].[Eartag], [CaseRelation].[Eartag]) AS [Eartag],
		ISNULL([sirePedigree].[Name], [CaseRelation].[Sire]) AS [Sire]
	FROM
		[lnkBatchCase] INNER JOIN [CaseRelation] ON [CaseRelation].[RBSE] = [lnkBatchCase].[RBSE]  
		INNER JOIN [luRelationType] ON [CaseRelation].[RelationType] = [luRelationType].[Code]
		LEFT JOIN [luSex] [luRelationSex] ON [CaseRelation].[Sex] = [luRelationSex].[Code]
		LEFT JOIN [luRelationFate] ON [CaseRelation].[RelationFate] = [luRelationFate].[Code]
		LEFT JOIN [Case] ON [CaseRelation].[RelationRBSE] = [Case].[RBSE]
		LEFT JOIN [luSex] [luCaseSex] ON [Case].[Sex] = [luCaseSex].[Code]
		LEFT JOIN [luCaseFate] ON [Case].[Fate] = [luCaseFate].[Code]
		LEFT JOIN [Pedigree] [casePedigree] ON [Case].[RBSE] = [casePedigree].[RBSE]
		LEFT JOIN [Pedigree] [sirePedigree] ON [casePedigree].[SireID] = [sirePedigree].[ID]
	WHERE
		[lnkBatchCase].[BatchID] = @BatchID

		SET NOCOUNT OFF

 
