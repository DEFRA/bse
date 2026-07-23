

CREATE PROCEDURE GetCPHHRBSEForBatchID 
	@BatchID int
AS
SELECT 
	[Case].[RBSE],
	LEFT([Case].[RBSE], 2) + '/' + SUBSTRING([Case].[RBSE], 3, 2) + '/' + RIGHT([Case].[RBSE], 5) AS [DisplayRBSE],
	[Case].[CPHH],
	LEFT([Case].[CPHH], 2) + '/' + SUBSTRING([Case].[CPHH], 3, 3) + '/' + SUBSTRING([Case].[CPHH], 6, 4) + '/' + RIGHT([Case].[CPHH], 2) AS [DisplayCPHH]
FROM  
	[lnkBatchCase] INNER JOIN [Case] ON [lnkBatchCase].[RBSE] = [Case].[RBSE]
WHERE
	[lnkBatchCase].[BatchID] = @BatchID
