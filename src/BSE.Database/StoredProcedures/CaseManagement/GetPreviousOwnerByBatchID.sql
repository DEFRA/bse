
CREATE PROCEDURE GetPreviousOwnerByBatchID
	@BatchID int
AS
	
SELECT 
	[lnkBatchCase].[RBSE],
	[CaseAggregate].[Name] AS [PreviousOwner]
FROM
	[lnkBatchCase]
	LEFT JOIN 
	(
		SELECT [RBSE],
			  [Name]
		FROM 
			 [OtherOwner]
		WHERE 
			 [Type] = 'PREVIOUS'
	)CaseAggregate ON [lnkBatchCase].[RBSE] = [CaseAggregate].[RBSE]

WHERE
	[lnkBatchCase].[BatchID] = @BatchID

ORDER BY 
	[lnkBatchCase].[RBSE]
