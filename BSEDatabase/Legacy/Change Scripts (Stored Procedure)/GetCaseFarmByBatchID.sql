set ANSI_NULLS ON
set QUOTED_IDENTIFIER ON
go


ALTER PROCEDURE [dbo].[GetCaseFarmByBatchID] 
	@BatchID int
AS
	
SELECT 
	[Case].[RBSE],
	LEFT([Case].[RBSE], 2) + '/' + SUBSTRING([Case].[RBSE], 3, 2) + '/' + RIGHT([Case].[RBSE], 5) AS [DisplayRBSE],
	[Case].[CPHH],
	LEFT([case].[CPHH], 2) + '/' + SUBSTRING([Case].[CPHH], 3, 3) + '/' + SUBSTRING([Case].[CPHH], 6, 4) + '/' + RIGHT([Case].[CPHH], 2) AS [DisplayCPHH],
	[Case].[EarTag],
	[Case].[EartagCountry] + [Case].[EartagHerdMark] As [EartagHerdMark],
	CONVERT(varchar,[Case].[BirthDate],103) AS BirthDate,
	[Case].[IsBAB],
	[Case].[OnsetAgeInMonths],
	[Case].[Sex],
	[Case].[Breed],
	[Case].[Origin],
	CONVERT(varchar,[Case].[PurchaseDate],103) AS PurchaseDate,
	[Case].[PurchaseAgeInMonths],
	CONVERT(varchar,[Case].[HerdEntryDate],103) AS HerdEntryDate,
	CONVERT(varchar,[Case].[OnsetDate],103) AS OnsetDate,
	[Case].[MonthsPregnant],
	[Case].[MonthsPostCalving],
	[Farm].[Ownername],
	[Farm].[Address1],
	[Farm].[HerdType],
	[Farm].[PedigreeType]
FROM
	[lnkBatchCase] INNER JOIN [Case] ON [lnkBatchCase].[RBSE] = [Case].[RBSE]
	INNER JOIN [Farm] ON [Case].[CPHH] = [Farm].[CPHH]
WHERE
	[lnkBatchCase].[BatchID] = @BatchID

ORDER BY 
	[Case].[RBSE]

 