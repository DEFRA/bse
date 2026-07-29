
CREATE PROCEDURE GetLatestBatchNumbers AS

	SELECT
		CONVERT(varchar(5),[BatchYear]) + '/' + CONVERT(varchar(10), [BatchNumber]) AS [Batch],
		[CaseCount]
	FROM
		[Batch]INNER JOIN 
	(SELECT TOP 3
		[Batch].[BatchID],
		COUNT([RBSE]) AS CaseCount
	FROM
		[Batch] LEFT JOIN [lnkBatchCase] On [Batch].[BatchID] = [lnkBatchCase].[BatchID]
	WHERE
		[Document] = 'BSE1' OR [Document] IS NULL
	GROUP BY
		[Batch].[BatchID]
	ORDER BY
		[Batch].[BatchID] DESC) AS [ttblBatchCase] ON [Batch].[BatchID] = [ttblBatchCase].[BatchID]
