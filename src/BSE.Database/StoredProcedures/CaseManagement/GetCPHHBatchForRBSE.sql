CREATE PROCEDURE GetCPHHBatchForRBSE
	@RBSE char(9) AS

SELECT
	[Batch].[BatchNumber],
	[Batch].[BatchYear],
	[Case].[CPHH]
FROM
	[Case] INNER JOIN [lnkBatchCase] ON [Case].[RBSE] = [lnkBatchCase].[RBSE]
	INNER JOIN [Batch] ON [lnkBatchCase].[BatchID] = [Batch].[BatchID]
WHERE
	[Case].[RBSE] = @RBSE AND
	[lnkBatchCase].[Document] = 'BSE1'