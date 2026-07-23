CREATE PROCEDURE GetCPHHRBSEForBatch
	@BatchYear smallint,
	@BatchNumber int AS

SELECT
	[Case].[RBSE],
	[Case].[CPHH]
FROM
	[Case] INNER JOIN [lnkBatchCase] ON [Case].[RBSE] = [lnkBatchCase].[RBSE]
	INNER JOIN [Batch] ON [lnkBatchCase].[BatchID] = [Batch].[BatchID]
WHERE
	[Batch].[BatchYear] = @BatchYear AND
	[Batch].[BatchNumber] = @BatchNumber