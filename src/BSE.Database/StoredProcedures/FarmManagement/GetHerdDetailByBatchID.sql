

CREATE PROCEDURE GetHerdDetailByBatchID
	@BatchID int
AS
SELECT
	[Case].[RBSE],
	[HerdSize].[CPHH],
	[HerdSize].[HerdYear],
	[HerdSize].[TotalSize],
	[HerdSize].[Lactation1Size],
	[HerdSize].[Lactation2Size],
	[HerdSize].[Lactation3Size],
	[HerdSize].[Lactation4Size],
	[HerdSize].[Lactation5Size],
	[HerdSize].[Lactation6Size],
	[HerdSize].[Lactation7Size],
	[HerdSize].[Lactation8Size],
	[HerdSize].[Lactation9Size],
	[HerdSize].[Lactation10Size],
	[HerdSize].[Lactation10PlusSize]
FROM
	[lnkBatchCase] INNER JOIN [Case] ON [lnkBatchCase].[RBSE] = [Case].[RBSE]
	INNER JOIN [Farm] ON [Case].[CPHH] = [Farm].[CPHH]
	LEFT JOIN [HerdSize] ON [Farm].[CPHH] = [HerdSize].[CPHH]
WHERE
	[lnkBatchCase].[BatchID] = @BatchID
